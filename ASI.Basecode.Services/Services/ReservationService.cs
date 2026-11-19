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
        private readonly IReservationRepository _reservationRepository;
        private readonly ISettingsService _settingsService;
        // Restricts a borrower with an unresolved penalty past its deadline, or an item
        // still out more than 3 days past its due date. Never lifts a restriction (admin only).
        private void RefreshRestriction(int borrowerDbId)
        {
            var user = _reservationRepository.GetUserByDbId(borrowerDbId);
            if (user == null || user.BorrowerStatus == PenaltyRules.Restricted) return;

            var now = DateTime.Now;
            var cutoff = now.AddDays(-PenaltyRules.SettleLateWithinDays);

            var pastDeadline = _reservationRepository.GetPenalties().Any(p =>
                p.BorrowerId == borrowerDbId && p.Status == PenaltyRules.Unresolved && p.DueBy < now);

            var notReturned = _reservationRepository.GetTransactions().Any(t =>
                t.BorrowerId == borrowerDbId && t.ActualReturnTime == null &&
                t.Reservation.EstimatedReturnDate < cutoff);

            if (pastDeadline || notReturned)
            {
                user.BorrowerStatus = PenaltyRules.Restricted;
                user.UpdatedTime = now;
                _reservationRepository.SaveChanges();
            }
        }
        public ReservationService(IReservationRepository reservationRepository, ISettingsService settingsService)
        {
            _reservationRepository = reservationRepository;
            _settingsService = settingsService;
        }

        public ReservationsPageViewModel GetPage(string search, int? categoryId, string custodianCode = null)
        {
            ExpireMissedPickups();

            var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            var reservations = _reservationRepository.GetReservations();
            var transactions = _reservationRepository.GetTransactions().Where(t => t.ActualReturnTime == null);

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
                var ownerDbId = _reservationRepository.GetUserDbId(custodianCode);
                reservations = reservations.Where(r => r.EquipmentItem.CustodianId == ownerDbId);
                transactions = transactions.Where(t => t.EquipmentItem.CustodianId == ownerDbId);
            }

            var settings = _settingsService.Get();


            return new ReservationsPageViewModel
            {
                Pending = reservations.Where(r => r.Status == "pending")
                                      .OrderBy(r => r.RequestedPickupDate).ToList(),
                Approved = reservations.Where(r => r.Status == "approved" && r.BorrowingTransaction == null)
                                       .OrderBy(r => r.RequestedPickupDate).ToList(),
                Active = transactions.OrderBy(t => t.ActualPickupTime).ToList(),
                Categories = _reservationRepository.GetCategories().OrderBy(c => c.CategoryName).ToList(),
                Search = search,
                CategoryId = categoryId,
                GraceMinutes = settings.OverdueGraceMinutes,
                DueSoonHours = settings.DueSoonHours
            };

        }

        // ---------- Student side ----------

        // All reservations of one borrower (userCode = Borrower.UserId, i.e. the session "UserId"), newest first
        public List<Reservation> GetForUser(string userCode)
        {
            ExpireMissedPickups();

            var borrowerDbId = _reservationRepository.GetUserDbId(userCode);
            if (borrowerDbId != 0) RefreshRestriction(borrowerDbId);

            return _reservationRepository.GetReservations()
                .Where(r => r.Borrower.UserId == userCode)
                .OrderByDescending(r => r.CreatedTime)
                .ToList();
        }

        // Items the student has picked up and not yet returned
        public int CountActiveForUser(string userCode)
        {
            return _reservationRepository.GetReservations().Count(r =>
                r.Borrower.UserId == userCode &&
                r.BorrowingTransaction != null &&
                r.BorrowingTransaction.ActualReturnTime == null);
        }

        // Requests still waiting for the custodian's decision
        public int CountPendingForUser(string userCode)
        {
            return _reservationRepository.GetReservations().Count(r =>
                r.Borrower.UserId == userCode &&
                r.Status == "pending");
        }
        public ServiceResult CreateReservation(string borrowerCode, int equipmentItemId, DateTime pickup, DateTime returnDate)
        {
            var borrowerDbId = _reservationRepository.GetUserDbId(borrowerCode);
            if (borrowerDbId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            ExpireMissedPickups();

            RefreshRestriction(borrowerDbId);
            if (_reservationRepository.GetUserByDbId(borrowerDbId)?.BorrowerStatus == PenaltyRules.Restricted)
                return ServiceResult.Fail("Your borrowing is restricted because of an unresolved penalty (overdue or damaged equipment). Settle it with the custodian, then ask an administrator to reactivate your account.");

            var item = _reservationRepository.GetEquipmentItemById(equipmentItemId);
            if (item == null || item.Status == "archived")
                return ServiceResult.Fail("This equipment item was not found.");
            if (item.Status != "available")
                return ServiceResult.Fail("This item is not available for reservation right now.");
            if (item.CustodianId.HasValue && !_reservationRepository.IsAccountActive(item.CustodianId.Value))
                return ServiceResult.Fail("This item is not available for reservation right now.");

            if (pickup == default || returnDate == default)
                return ServiceResult.Fail("Please choose both a pickup and a return date and time.");

            // The custodian's rules: duration, how far ahead, operating hours
            var rules = _settingsService.Get();
            var scheduleError = BorrowingRules.CheckSchedule(rules, pickup, returnDate, DateTime.Now);
            if (scheduleError != null)
                return ServiceResult.Fail(scheduleError);

            // Item limit: pending requests, approved reservations and items not yet returned all count
            var holding = _reservationRepository.GetReservations().Count(x =>
                x.BorrowerId == borrowerDbId &&
                (x.Status == "pending" ||
                 (x.Status == "approved" &&
                  (x.BorrowingTransaction == null || x.BorrowingTransaction.ActualReturnTime == null))));
            if (holding >= rules.MaxActiveItems)
                return ServiceResult.Fail(
                    $"You already have {holding} request(s) or item(s) in progress. The limit is {rules.MaxActiveItems}. " +
                    "Wait for a decision, cancel a request, or return an item first.");

            // One open request per student per item
            var alreadyRequested = _reservationRepository.GetReservations().Any(x =>
                x.BorrowerId == borrowerDbId &&
                x.EquipmentItemId == equipmentItemId &&
                x.Status == "pending");
            if (alreadyRequested)
                return ServiceResult.Fail("You already have a pending request for this item.");

            // Not allowed to overlap a reservation the custodian already approved (and that isn't returned yet)
            var overlapsApproved = _reservationRepository.GetReservations().Any(x =>
                x.EquipmentItemId == equipmentItemId &&
                x.Status == "approved" &&
                (x.BorrowingTransaction == null || x.BorrowingTransaction.ActualReturnTime == null) &&
                x.RequestedPickupDate < returnDate &&
                x.EstimatedReturnDate > pickup);
            if (overlapsApproved)
                return ServiceResult.Fail("This item is already reserved for part of that time. Please choose different dates.");

            var now = DateTime.Now;
            _reservationRepository.AddReservation(new Reservation
            {
                BorrowerId = borrowerDbId,
                EquipmentItemId = equipmentItemId,
                RequestedPickupDate = pickup,
                EstimatedReturnDate = returnDate,
                Status = "pending",
                CreatedTime = now,
                UpdatedTime = now
            });
            _reservationRepository.SaveChanges();

            return ServiceResult.Ok("Request sent! The custodian will review it and you will see the result here.");
        }

        public ServiceResult CancelReservation(int reservationId, string borrowerCode)
        {
            var borrowerDbId = _reservationRepository.GetUserDbId(borrowerCode);
            if (borrowerDbId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            var reservation = _reservationRepository.GetReservationById(reservationId);
            if (reservation == null || reservation.BorrowerId != borrowerDbId)
                return ServiceResult.Fail("Reservation not found.");

            // Once the item has been handed over it can no longer be cancelled
            var canCancel = reservation.BorrowingTransaction == null &&
                            (reservation.Status == "pending" || reservation.Status == "approved");
            if (!canCancel)
                return ServiceResult.Fail("This reservation can no longer be cancelled.");

            reservation.Status = "cancelled";
            reservation.ReviewNotes = null;   // a cancelled request that has a note was cancelled by someone else
            reservation.UpdatedTime = DateTime.Now;

            try
            {
                _reservationRepository.SaveChanges();
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
            var custodianDbId = _reservationRepository.GetUserDbId(custodianCode);
            if (custodianDbId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            ExpireMissedPickups();

            var reservation = _reservationRepository.GetReservationById(reservationId);
            if (reservation == null) return ServiceResult.Fail("Reservation not found.");
            if (reservation.EquipmentItem.CustodianId != custodianDbId)
                return ServiceResult.Fail("This request belongs to another custodian.");
            if (reservation.Status != "pending") return ServiceResult.Fail("This request was already processed.");
            if (reservation.Borrower?.BorrowerStatus == PenaltyRules.Restricted)
                return ServiceResult.Fail("This borrower is restricted because of an unresolved penalty.");
            if (reservation.EquipmentItem.Status != "available")
                return ServiceResult.Fail("This item is not available right now.");

            // Only an approved reservation that covers part of the same time blocks this one
            // (the same rule students are held to when they request)
            var overlapsApproved = _reservationRepository.GetReservations().Any(x =>
                x.Id != reservation.Id && x.EquipmentItemId == reservation.EquipmentItemId &&
                x.Status == "approved" && x.BorrowingTransaction == null &&
                x.RequestedPickupDate < reservation.EstimatedReturnDate &&
                x.EstimatedReturnDate > reservation.RequestedPickupDate);
            if (overlapsApproved)
                return ServiceResult.Fail("Another approved reservation already covers part of this time for the item. Cancel that one first, or decline this request.");
            var limit = _settingsService.Get().MaxActiveItems;
            var holding = _reservationRepository.GetReservations().Count(x =>
                x.Id != reservation.Id &&
                x.BorrowerId == reservation.BorrowerId &&
                x.Status == "approved" &&
                (x.BorrowingTransaction == null || x.BorrowingTransaction.ActualReturnTime == null));
            if (holding >= limit)
                return ServiceResult.Fail($"This borrower already has {holding} approved or borrowed item(s). The limit is {limit}. Decline this request, or approve it after an item is returned.");
            reservation.Status = "approved";
            reservation.ReviewedBy = custodianDbId;
            reservation.ReviewedAt = DateTime.Now;
            reservation.ReviewNotes = notes;
            reservation.UpdatedTime = DateTime.Now;
            _reservationRepository.SaveChanges();

            return ServiceResult.Ok("Request approved. Release the item when the borrower arrives.");
        }

        public ServiceResult Decline(int reservationId, string reason, string custodianCode)
        {
            var custodianDbId = _reservationRepository.GetUserDbId(custodianCode);
            if (custodianDbId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            var reservation = _reservationRepository.GetReservationById(reservationId);
            if (reservation == null) return ServiceResult.Fail("Reservation not found.");
            if (reservation.EquipmentItem.CustodianId != custodianDbId)
                return ServiceResult.Fail("This request belongs to another custodian.");
            if (reservation.Status != "pending") return ServiceResult.Fail("This request was already processed.");

            reservation.Status = "declined";
            reservation.ReviewedBy = custodianDbId;
            reservation.ReviewedAt = DateTime.Now;
            reservation.ReviewNotes = reason;
            reservation.UpdatedTime = DateTime.Now;
            _reservationRepository.SaveChanges();

            return ServiceResult.Ok("Request declined.");
        }

        // The custodian cancels an approved reservation that hasn't been picked up (no-show, maintenance, ...)
        public ServiceResult CancelApproved(int reservationId, string reason, string custodianCode)
        {
            var custodianDbId = _reservationRepository.GetUserDbId(custodianCode);
            if (custodianDbId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            var reservation = _reservationRepository.GetReservationById(reservationId);
            if (reservation == null) return ServiceResult.Fail("Reservation not found.");
            if (reservation.EquipmentItem.CustodianId != custodianDbId)
                return ServiceResult.Fail("This reservation belongs to another custodian.");
            if (reservation.Status != "approved" || reservation.BorrowingTransaction != null)
                return ServiceResult.Fail("Only an approved reservation that hasn't been picked up can be cancelled.");

            reason = (reason ?? "").Trim();
            if (reason.Length < 3 || reason.Length > 255)
                return ServiceResult.Fail("Please give a reason (3 to 255 characters).");

            var now = DateTime.Now;
            reservation.Status = "cancelled";
            reservation.ReviewNotes = reason;
            reservation.ReviewedBy = custodianDbId;
            reservation.ReviewedAt = now;
            reservation.UpdatedTime = now;

            try
            {
                _reservationRepository.SaveChanges();
            }
            catch (Exception)
            {
                return ServiceResult.Fail("Could not cancel the reservation. Please try again.");
            }

            return ServiceResult.Ok("Reservation cancelled. The borrower will see your reason.");
        }

        // An approved reservation that was never picked up expires once its return time has passed,
        // so a no-show can't keep blocking the item. Runs whenever reservations are read or changed.
        private void ExpireMissedPickups()
        {
            var now = DateTime.Now;
            var missed = _reservationRepository.GetReservations()
                .Where(r => r.Status == "approved" && r.BorrowingTransaction == null && r.EstimatedReturnDate < now)
                .ToList();
            if (missed.Count == 0) return;

            foreach (var r in missed)
            {
                r.Status = "cancelled";
                r.ReviewNotes = "Expired: the item was not picked up before the return time.";
                r.UpdatedTime = now;
            }
            _reservationRepository.SaveChanges();
        }

        public ServiceResult Release(int reservationId, string custodianCode)
        {
            var custodianDbId = _reservationRepository.GetUserDbId(custodianCode);
            if (custodianDbId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            ExpireMissedPickups();

            var reservation = _reservationRepository.GetReservationById(reservationId);
            if (reservation == null) return ServiceResult.Fail("Reservation not found.");
            if (reservation.EquipmentItem.CustodianId != custodianDbId)
                return ServiceResult.Fail("This request belongs to another custodian.");
            if (reservation.Status != "approved" || reservation.BorrowingTransaction != null)
                return ServiceResult.Fail("This reservation can't be released. It may have expired or been cancelled.");
            if (reservation.EquipmentItem.Status != "available")
                return ServiceResult.Fail("This item is not available right now.");

            _reservationRepository.AddTransaction(new BorrowingTransaction
            {
                ReservationId = reservation.Id,
                EquipmentItemId = reservation.EquipmentItemId,
                BorrowerId = reservation.BorrowerId,
                ReleasedBy = custodianDbId,
                ActualPickupTime = DateTime.Now,
                IsOverdue = false,
                DelayDurationHours = 0,
                CreatedTime = DateTime.Now,
                UpdatedTime = DateTime.Now
            });

            reservation.EquipmentItem.Status = "borrowed";
            reservation.EquipmentItem.UpdatedTime = DateTime.Now;
            _reservationRepository.SaveChanges();

            return ServiceResult.Ok("Item released to the borrower.");
        }

        public ServiceResult Return(int transactionId, string condition, string notes, string custodianCode)
        {
            var custodianDbId = _reservationRepository.GetUserDbId(custodianCode);
            if (custodianDbId == 0) return ServiceResult.Fail("Your session expired. Please log in again.");

            var transaction = _reservationRepository.GetTransactionById(transactionId);
            if (transaction == null) return ServiceResult.Fail("Transaction not found.");
            if (transaction.EquipmentItem.CustodianId != custodianDbId)
                return ServiceResult.Fail("This loan belongs to another custodian.");
            if (transaction.ActualReturnTime != null) return ServiceResult.Fail("This item was already returned.");

            var now = DateTime.Now;
            transaction.ActualReturnTime = now;
            transaction.ReturnedTo = custodianDbId;
            transaction.ReturnCondition = condition;
            transaction.DamageNotes = notes;
            transaction.UpdatedTime = now;

            var due = transaction.Reservation?.EstimatedReturnDate;
            if (due.HasValue)
            {
                var lateBy = now - due.Value;
                if (lateBy.TotalMinutes > _settingsService.Get().OverdueGraceMinutes)
                {
                    transaction.IsOverdue = true;
                    transaction.DelayDurationHours = (int)Math.Ceiling(lateBy.TotalHours);
                }
            }
            // ---- Penalties ----
            if (transaction.IsOverdue && due.HasValue)
            {
                var days = PenaltyRules.LateDays(due.Value, now);
                _reservationRepository.AddPenalty(new Penalty
                {
                    TransactionId = transaction.Id,
                    BorrowerId = transaction.BorrowerId,
                    PenaltyType = PenaltyRules.Late,
                    Amount = days * PenaltyRules.LateFeePerDay,
                    Notes = $"{days} day(s) late",
                    Status = PenaltyRules.Unresolved,
                    DueBy = now.AddDays(PenaltyRules.SettleLateWithinDays),
                    CreatedTime = now
                });
            }

            if (PenaltyRules.IsDamage(condition))
            {
                _reservationRepository.AddPenalty(new Penalty
                {
                    TransactionId = transaction.Id,
                    BorrowerId = transaction.BorrowerId,
                    PenaltyType = PenaltyRules.Damage,
                    Amount = 0,
                    Notes = notes,
                    Status = PenaltyRules.Unresolved,
                    DueBy = now.AddDays(PenaltyRules.ReplaceWithinDays),
                    CreatedTime = now
                });
            }

            transaction.EquipmentItem.ConditionRating = condition;
            transaction.EquipmentItem.Status = condition == "needs_maintenance" ? "maintenance" : "available";
            transaction.EquipmentItem.UpdatedTime = now;
            _reservationRepository.SaveChanges();

            return ServiceResult.Ok("Item marked as returned.");
        }

    }
}