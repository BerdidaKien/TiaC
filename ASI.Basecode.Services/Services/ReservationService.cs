using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _repo;
        private readonly ISettingsService _settings;

        public ReservationService(IReservationRepository repo, ISettingsService settings)
        {
            _repo = repo;
            _settings = settings;
        }

        public ReservationsPageViewModel GetPage(string search, int? categoryId, string custodianCode = null)
        {
            var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            var reservations = _repo.GetReservations();
            var transactions = _repo.GetTransactions().Where(t => t.ActualReturnTime == null);

            if (term != null)
            {
                reservations = reservations.Where(r =>
                    r.EquipmentItem.ItemCode.Contains(term) ||
                    r.EquipmentItem.BrandModel.Contains(term) ||
                    r.Borrower.FirstName.Contains(term) ||
                    r.Borrower.LastName.Contains(term) ||
                    r.Borrower.UserId.Contains(term));

                transactions = transactions.Where(t =>
                    t.EquipmentItem.ItemCode.Contains(term) ||
                    t.EquipmentItem.BrandModel.Contains(term) ||
                    t.Borrower.FirstName.Contains(term) ||
                    t.Borrower.LastName.Contains(term) ||
                    t.Borrower.UserId.Contains(term));
            }

            if (categoryId.HasValue)
            {
                reservations = reservations.Where(r => r.EquipmentItem.CategoryId == categoryId.Value);
                transactions = transactions.Where(t => t.EquipmentItem.CategoryId == categoryId.Value);
            }

            // A custodian only sees requests and loans for their own equipment
            if (!string.IsNullOrEmpty(custodianCode))
            {
                var ownerId = _repo.GetUserDbId(custodianCode);
                reservations = reservations.Where(r => r.EquipmentItem.CustodianId == ownerId);
                transactions = transactions.Where(t => t.EquipmentItem.CustodianId == ownerId);
            }

            var s = _settings.Get();


            return new ReservationsPageViewModel
            {
                Pending = reservations.Where(r => r.Status == "pending")
                                      .OrderBy(r => r.RequestedPickupDate).ToList(),
                Approved = reservations.Where(r => r.Status == "approved" && r.BorrowingTransaction == null)
                                       .OrderBy(r => r.RequestedPickupDate).ToList(),
                Active = transactions.OrderBy(t => t.ActualPickupTime).ToList(),
                Categories = _repo.GetCategories().OrderBy(c => c.CategoryName).ToList(),
                Search = search,
                CategoryId = categoryId,
                GraceMinutes = s.OverdueGraceMinutes,
                DueSoonHours = s.DueSoonHours
            };

        }

        // ---------- Student side ----------

        // All reservations of one borrower (userCode = Borrower.UserId, i.e. the session "UserId"), newest first
        public List<Reservation> GetForUser(string userCode)
        {
            return _repo.GetReservations()
                .Where(r => r.Borrower.UserId == userCode)
                .OrderByDescending(r => r.CreatedTime)
                .ToList();
        }

        // Items the student has picked up and not yet returned
        public int CountActiveForUser(string userCode)
        {
            return _repo.GetReservations().Count(r =>
                r.Borrower.UserId == userCode &&
                r.BorrowingTransaction != null &&
                r.BorrowingTransaction.ActualReturnTime == null);
        }

        // Requests still waiting for the custodian's decision
        public int CountPendingForUser(string userCode)
        {
            return _repo.GetReservations().Count(r =>
                r.Borrower.UserId == userCode &&
                r.Status == "pending");
        }
        public ServiceResult CreateReservation(string borrowerCode, int equipmentItemId, DateTime pickup, DateTime returnDate)
        {
            var borrowerId = _repo.GetUserDbId(borrowerCode);
            if (borrowerId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            var item = _repo.GetEquipmentItemById(equipmentItemId);
            if (item == null || item.Status == "archived")
                return ServiceResult.Fail("This equipment item was not found.");
            if (item.Status != "available")
                return ServiceResult.Fail("This item is not available for reservation right now.");

            if (pickup == default || returnDate == default)
                return ServiceResult.Fail("Please choose both a pickup and a return date and time.");

            // The custodian's rules: duration, how far ahead, operating hours
            var rules = _settings.Get();
            var scheduleError = BorrowingRules.CheckSchedule(rules, pickup, returnDate, DateTime.Now);
            if (scheduleError != null)
                return ServiceResult.Fail(scheduleError);

            // Item limit: pending requests, approved reservations and items not yet returned all count
            var holding = _repo.GetReservations().Count(x =>
                x.BorrowerId == borrowerId &&
                (x.Status == "pending" ||
                 (x.Status == "approved" &&
                  (x.BorrowingTransaction == null || x.BorrowingTransaction.ActualReturnTime == null))));
            if (holding >= rules.MaxActiveItems)
                return ServiceResult.Fail(
                    $"You already have {holding} request(s) or item(s) in progress. The limit is {rules.MaxActiveItems}. " +
                    "Wait for a decision, cancel a request, or return an item first.");

            // One open request per student per item
            var alreadyRequested = _repo.GetReservations().Any(x =>
                x.BorrowerId == borrowerId &&
                x.EquipmentItemId == equipmentItemId &&
                x.Status == "pending");
            if (alreadyRequested)
                return ServiceResult.Fail("You already have a pending request for this item.");

            // Not allowed to overlap a reservation the custodian already approved (and that isn't returned yet)
            var overlapsApproved = _repo.GetReservations().Any(x =>
                x.EquipmentItemId == equipmentItemId &&
                x.Status == "approved" &&
                (x.BorrowingTransaction == null || x.BorrowingTransaction.ActualReturnTime == null) &&
                x.RequestedPickupDate < returnDate &&
                x.EstimatedReturnDate > pickup);
            if (overlapsApproved)
                return ServiceResult.Fail("This item is already reserved for part of that time. Please choose different dates.");

            var now = DateTime.Now;
            _repo.AddReservation(new Reservation
            {
                BorrowerId = borrowerId,
                EquipmentItemId = equipmentItemId,
                RequestedPickupDate = pickup,
                EstimatedReturnDate = returnDate,
                Status = "pending",
                CreatedTime = now,
                UpdatedTime = now
            });
            _repo.SaveChanges();

            return ServiceResult.Ok("Request sent! The custodian will review it and you will see the result here.");
        }

        public ServiceResult CancelReservation(int reservationId, string borrowerCode)
        {
            var borrowerId = _repo.GetUserDbId(borrowerCode);
            if (borrowerId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            var r = _repo.GetReservationById(reservationId);
            if (r == null || r.BorrowerId != borrowerId)
                return ServiceResult.Fail("Reservation not found.");

            // Once the item has been handed over it can no longer be cancelled
            var canCancel = r.BorrowingTransaction == null &&
                            (r.Status == "pending" || r.Status == "approved");
            if (!canCancel)
                return ServiceResult.Fail("This reservation can no longer be cancelled.");

            r.Status = "cancelled";
            r.UpdatedTime = DateTime.Now;

            try
            {
                _repo.SaveChanges();
            }
            catch (Exception)
            {
                return ServiceResult.Fail("Could not cancel the reservation. Please try again.");
            }

            return ServiceResult.Ok("Your reservation was cancelled.");
        }

        // ---------- Custodian actions ----------

        public ServiceResult Approve(int reservationId, string notes, string custodianCode)
        {
            var custodianId = _repo.GetUserDbId(custodianCode);
            if (custodianId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            var r = _repo.GetReservationById(reservationId);
            if (r == null) return ServiceResult.Fail("Reservation not found.");
            if (r.EquipmentItem.CustodianId != custodianId)
                return ServiceResult.Fail("This request belongs to another custodian.");
            if (r.Status != "pending") return ServiceResult.Fail("This request was already processed.");
            if (r.EquipmentItem.Status != "available")
                return ServiceResult.Fail("This item is not available right now.");

            var alreadyReserved = _repo.GetReservations().Any(x =>
                x.Id != r.Id && x.EquipmentItemId == r.EquipmentItemId &&
                x.Status == "approved" && x.BorrowingTransaction == null);
            if (alreadyReserved)
                return ServiceResult.Fail("Another approved request is already waiting to pick up this item.");
            var limit = _settings.Get().MaxActiveItems;
            var holding = _repo.GetReservations().Count(x =>
                x.Id != r.Id &&
                x.BorrowerId == r.BorrowerId &&
                x.Status == "approved" &&
                (x.BorrowingTransaction == null || x.BorrowingTransaction.ActualReturnTime == null));
            if (holding >= limit)
                return ServiceResult.Fail($"This borrower already has {holding} approved or borrowed item(s). The limit is {limit}. Decline this request, or approve it after an item is returned.");
            r.Status = "approved";
            r.ReviewedBy = custodianId;
            r.ReviewedAt = DateTime.Now;
            r.ReviewNotes = notes;
            r.UpdatedTime = DateTime.Now;
            _repo.SaveChanges();

            return ServiceResult.Ok("Request approved. Release the item when the borrower arrives.");
        }

        public ServiceResult Decline(int reservationId, string reason, string custodianCode)
        {
            var custodianId = _repo.GetUserDbId(custodianCode);
            if (custodianId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            var r = _repo.GetReservationById(reservationId);
            if (r == null) return ServiceResult.Fail("Reservation not found.");
            if (r.EquipmentItem.CustodianId != custodianId)
                return ServiceResult.Fail("This request belongs to another custodian.");
            if (r.Status != "pending") return ServiceResult.Fail("This request was already processed.");

            r.Status = "declined";
            r.ReviewedBy = custodianId;
            r.ReviewedAt = DateTime.Now;
            r.ReviewNotes = reason;
            r.UpdatedTime = DateTime.Now;
            _repo.SaveChanges();

            return ServiceResult.Ok("Request declined.");
        }

        public ServiceResult Release(int reservationId, string custodianCode)
        {
            var custodianId = _repo.GetUserDbId(custodianCode);
            if (custodianId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            var r = _repo.GetReservationById(reservationId);
            if (r == null) return ServiceResult.Fail("Reservation not found.");
            if (r.EquipmentItem.CustodianId != custodianId)
                return ServiceResult.Fail("This request belongs to another custodian.");
            if (r.Status != "approved" || r.BorrowingTransaction != null)
                return ServiceResult.Fail("This reservation can't be released.");
            if (r.EquipmentItem.Status != "available")
                return ServiceResult.Fail("This item is not available right now.");

            _repo.AddTransaction(new BorrowingTransaction
            {
                ReservationId = r.Id,
                EquipmentItemId = r.EquipmentItemId,
                BorrowerId = r.BorrowerId,
                ReleasedBy = custodianId,
                ActualPickupTime = DateTime.Now,
                IsOverdue = false,
                DelayDurationHours = 0,
                CreatedTime = DateTime.Now,
                UpdatedTime = DateTime.Now
            });

            r.EquipmentItem.Status = "borrowed";
            r.EquipmentItem.UpdatedTime = DateTime.Now;
            _repo.SaveChanges();

            return ServiceResult.Ok("Item released to the borrower.");
        }

        public ServiceResult Return(int transactionId, string condition, string notes, string custodianCode)
        {
            var custodianId = _repo.GetUserDbId(custodianCode);
            if (custodianId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            var t = _repo.GetTransactionById(transactionId);
            if (t == null) return ServiceResult.Fail("Transaction not found.");
            if (t.EquipmentItem.CustodianId != custodianId)
                return ServiceResult.Fail("This loan belongs to another custodian.");
            if (t.ActualReturnTime != null) return ServiceResult.Fail("This item was already returned.");

            var now = DateTime.Now;
            t.ActualReturnTime = now;
            t.ReturnedTo = custodianId;
            t.ReturnCondition = condition;
            t.DamageNotes = notes;
            t.UpdatedTime = now;

            var due = t.Reservation?.EstimatedReturnDate;
            if (due.HasValue)
            {
                var lateBy = now - due.Value;
                if (lateBy.TotalMinutes > _settings.Get().OverdueGraceMinutes)
                {
                    t.IsOverdue = true;
                    t.DelayDurationHours = (int)Math.Ceiling(lateBy.TotalHours);
                }
            }

            t.EquipmentItem.ConditionRating = condition;
            t.EquipmentItem.Status = condition == "needs_maintenance" ? "maintenance" : "available";
            t.EquipmentItem.UpdatedTime = now;
            _repo.SaveChanges();

            return ServiceResult.Ok("Item marked as returned.");
        }
    }
}