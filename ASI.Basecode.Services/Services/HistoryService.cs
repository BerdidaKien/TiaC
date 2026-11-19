
using System;
using System.Linq;
using System.Text;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Services
{
    public class HistoryService : IHistoryService
    {
        private readonly IReservationRepository _reservationRepository;

        public HistoryService(IReservationRepository reservationRepository)
        {
            _reservationRepository = reservationRepository;
        }

        // Search, category, and date filters.
        // Student ID is optional because Custodian History
        // needs to retrieve records from all borrowers.
        private IQueryable<BorrowingTransaction> BuildBase(
            HistoryFilter f,
            string borrowerCode = null,
            string custodianCode = null)
        {
            var q = _reservationRepository.GetTransactions();

            // Custodian view: only loans of equipment this custodian owns
            if (!string.IsNullOrEmpty(custodianCode))
            {
                var ownerDbId = _reservationRepository.GetUserDbId(custodianCode);
                q = q.Where(t => t.EquipmentItem.CustodianId == ownerDbId);
            }

            // Restrict records to the logged-in student.
            if (borrowerCode != null)
            {
                q = q.Where(t =>
                    t.Borrower.UserId == borrowerCode);
            }

            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var term = f.Search.Trim();

                q = q.Where(t =>
                    t.Borrower.UserId.Contains(term)
                    || t.Borrower.FirstName.Contains(term)
                    || t.Borrower.LastName.Contains(term)
                    || t.EquipmentItem.ItemCode.Contains(term)
                    || t.EquipmentItem.BrandModel.Contains(term));
            }

            if (f.CategoryId.HasValue)
            {
                q = q.Where(t =>
                    t.EquipmentItem.CategoryId == f.CategoryId.Value);
            }

            if (f.DateFrom.HasValue)
            {
                var from = f.DateFrom.Value.Date;

                q = q.Where(t =>
                    t.ActualPickupTime >= from);
            }

            if (f.DateTo.HasValue)
            {
                var to = f.DateTo.Value.Date.AddDays(1);

                q = q.Where(t =>
                    t.ActualPickupTime < to);
            }

            return q;
        }

        private IQueryable<BorrowingTransaction> ApplyResult(
            IQueryable<BorrowingTransaction> q,
            string result)
        {
            var now = DateTime.Now;

            switch (result)
            {
                case "active":
                    return q.Where(t =>
                        t.ActualReturnTime == null);

                case "ontime":
                    return q.Where(t =>
                        t.ActualReturnTime != null &&
                        !t.IsOverdue);

                case "overdue":
                    return q.Where(t =>
                        t.IsOverdue ||
                        (t.ActualReturnTime == null &&
                         t.Reservation.EstimatedReturnDate < now));

                case "damaged":
                    return q.Where(t =>
                        t.ReturnCondition == "needs_maintenance");

                default:
                    return q;
            }
        }

        // Custodian History.
        // Retrieves all borrowers' transactions.
        public HistoryPageViewModel GetPage(HistoryFilter filter, string custodianCode = null)
        {
            return BuildPage(filter, null, custodianCode);
        }

        // Student History.
        // Retrieves only the logged-in student's transactions.
        public HistoryPageViewModel GetStudentPage(
            HistoryFilter filter,
            string borrowerCode)
        {
            var f = filter ?? new HistoryFilter();

            if (string.IsNullOrWhiteSpace(borrowerCode))
            {
                return new HistoryPageViewModel
                {
                    Filter = f,
                    Categories = _reservationRepository.GetCategories()
                        .OrderBy(c => c.CategoryName)
                        .ToList()
                };
            }

            return BuildPage(f, borrowerCode);
        }

        // Shared summary and result calculations.
        private HistoryPageViewModel BuildPage(
            HistoryFilter filter,
            string borrowerCode,
            string custodianCode = null)
        {
            var f = filter ?? new HistoryFilter();

            var baseQ = BuildBase(f, borrowerCode, custodianCode);
            var now = DateTime.Now;

            var returned = baseQ.Count(t =>
                t.ActualReturnTime != null);

            var lateReturns = baseQ.Count(t =>
                t.ActualReturnTime != null &&
                t.IsOverdue);
            var penaltyQ = _reservationRepository.GetPenalties().Where(p => p.Status == PenaltyRules.Unresolved);

            if (!string.IsNullOrEmpty(custodianCode))
            {
                var ownerDbId = _reservationRepository.GetUserDbId(custodianCode);
                penaltyQ = penaltyQ.Where(p => p.Transaction.EquipmentItem.CustodianId == ownerDbId);
            }
            if (borrowerCode != null)
            {
                penaltyQ = penaltyQ.Where(p => p.Borrower.UserId == borrowerCode);
            }

            var openPenalties = penaltyQ.OrderBy(p => p.DueBy).ToList();

            var charges = openPenalties
                .GroupBy(p => p.BorrowerId)
                .Select(g => new StudentChargeRow
                {
                    BorrowerId = g.Key,
                    UserCode = g.First().Borrower.UserId,
                    Name = g.First().Borrower.LastName + ", " + g.First().Borrower.FirstName,
                    LateTotal = g.Where(p => p.PenaltyType == PenaltyRules.Late).Sum(p => p.Amount),
                    LateCount = g.Count(p => p.PenaltyType == PenaltyRules.Late),
                    ReplacementCount = g.Count(p => p.PenaltyType == PenaltyRules.Damage),
                    EarliestDeadline = g.Min(p => p.DueBy),
                    PastDeadline = g.Any(p => p.DueBy < now),
                    Restricted = g.First().Borrower.BorrowerStatus == PenaltyRules.Restricted
                })
                .OrderByDescending(c => c.LateTotal)
                .ThenByDescending(c => c.ReplacementCount)
                .ToList();
            return new HistoryPageViewModel
            {
                Filter = f,
                StudentCharges = charges,
                OpenPenalties = openPenalties,
                Categories = _reservationRepository.GetCategories()
                    .OrderBy(c => c.CategoryName)
                    .ToList(),

                TotalTransactions = baseQ.Count(),

                ActiveLoans = baseQ.Count(t =>
                    t.ActualReturnTime == null),

                LateReturns = lateReturns,

                CurrentlyOverdue = baseQ.Count(t =>
                    t.ActualReturnTime == null &&
                    t.Reservation.EstimatedReturnDate < now),

                TotalDelayHours = baseQ
                    .Where(t =>
                        t.ActualReturnTime != null &&
                        t.IsOverdue)
                    .Sum(t =>
                        (int?)t.DelayDurationHours) ?? 0,

                DamagedReturns = baseQ.Count(t =>
                    t.ReturnCondition == "needs_maintenance"),

                OnTimeRate = returned == 0
                    ? 0
                    : (int)Math.Round(
                        (returned - lateReturns) * 100.0 / returned),

                Rows = ApplyResult(baseQ, f.Result)
                    .OrderByDescending(t => t.ActualPickupTime)
                    .ToList(),

                PenaltyRows = baseQ
                    .Where(t =>
                        t.IsOverdue ||
                        t.ReturnCondition == "needs_maintenance" ||
                        (t.ActualReturnTime == null &&
                         t.Reservation.EstimatedReturnDate < now))
                    .OrderByDescending(t => t.ActualPickupTime)
                    .Take(10)
                    .ToList()
            };
        }

        // Existing Custodian CSV export.
        public byte[] ExportCsv(HistoryFilter filter, string custodianCode = null)
        {
            var f = filter ?? new HistoryFilter();

            var rows = ApplyResult(BuildBase(f, null, custodianCode), f.Result)
                .OrderByDescending(t => t.ActualPickupTime)
                .ToList();

            var sb = new StringBuilder();

            sb.AppendLine(
                "Transaction ID,Item Code,Equipment,Category,Borrower ID,Borrower Name," +
                "Handover Time,Released By,Due Return,Returned Time,Received By," +
                "Return Condition,Overdue,Delay Hours,Damage Notes");

            foreach (var t in rows)
            {
                var cells = new[]
                {
                    HistoryPageViewModel.TxnCode(t),
                    t.EquipmentItem.ItemCode,
                    t.EquipmentItem.BrandModel,
                    t.EquipmentItem.Category?.CategoryName,
                    t.Borrower.UserId,
                    $"{t.Borrower.FirstName} {t.Borrower.LastName}",
                    $"{t.ActualPickupTime:yyyy-MM-dd HH:mm}",
                    t.ReleasedByNavigation == null
                        ? ""
                        : $"{t.ReleasedByNavigation.FirstName} {t.ReleasedByNavigation.LastName}",
                    $"{t.Reservation?.EstimatedReturnDate:yyyy-MM-dd HH:mm}",
                    $"{t.ActualReturnTime:yyyy-MM-dd HH:mm}",
                    t.ReturnedToNavigation == null
                        ? ""
                        : $"{t.ReturnedToNavigation.FirstName} {t.ReturnedToNavigation.LastName}",
                    t.ReturnCondition,
                    t.IsOverdue ? "Yes" : "No",
                    t.DelayDurationHours.ToString(),
                    t.DamageNotes
                };

                sb.AppendLine(string.Join(",", cells.Select(Csv)));
            }

            // UTF-8 with BOM for Excel compatibility.
            return Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
                .ToArray();
        }

        private static string Csv(string value)
        {
            value = value ?? "";

            // Prevent spreadsheet formula execution.
            if (value.Length > 0 &&
                "=+-@".IndexOf(value[0]) >= 0)
            {
                value = "'" + value;
            }

            if (value.IndexOfAny(
                new[] { ',', '"', '\n', '\r' }) >= 0)
            {
                value = "\"" +
                    value.Replace("\"", "\"\"") +
                    "\"";
            }

            return value;
        }
    }
}
