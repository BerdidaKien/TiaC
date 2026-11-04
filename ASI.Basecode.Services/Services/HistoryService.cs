
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
        private readonly IReservationRepository _repo;

        public HistoryService(IReservationRepository repo)
        {
            _repo = repo;
        }

        // Search, category, and date filters.
        // Student ID is optional because Custodian History
        // needs to retrieve records from all borrowers.
        private IQueryable<BorrowingTransaction> BuildBase(
            HistoryFilter f,
            string borrowerUserId = null)
        {
            var q = _repo.GetTransactions();

            // Restrict records to the logged-in student.
            if (borrowerUserId != null)
            {
                q = q.Where(t =>
                    t.Borrower.UserId == borrowerUserId);
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
        public HistoryPageViewModel GetPage(HistoryFilter filter)
        {
            return BuildPage(filter, null);
        }

        // Student History.
        // Retrieves only the logged-in student's transactions.
        public HistoryPageViewModel GetStudentPage(
            HistoryFilter filter,
            string borrowerUserId)
        {
            var f = filter ?? new HistoryFilter();

            if (string.IsNullOrWhiteSpace(borrowerUserId))
            {
                return new HistoryPageViewModel
                {
                    Filter = f,
                    Categories = _repo.GetCategories()
                        .OrderBy(c => c.CategoryName)
                        .ToList()
                };
            }

            return BuildPage(f, borrowerUserId);
        }

        // Shared summary and result calculations.
        private HistoryPageViewModel BuildPage(
            HistoryFilter filter,
            string borrowerUserId)
        {
            var f = filter ?? new HistoryFilter();

            var baseQ = BuildBase(f, borrowerUserId);
            var now = DateTime.Now;

            var returned = baseQ.Count(t =>
                t.ActualReturnTime != null);

            var lateReturns = baseQ.Count(t =>
                t.ActualReturnTime != null &&
                t.IsOverdue);

            return new HistoryPageViewModel
            {
                Filter = f,

                Categories = _repo.GetCategories()
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
        public byte[] ExportCsv(HistoryFilter filter)
        {
            var f = filter ?? new HistoryFilter();

            var rows = ApplyResult(BuildBase(f), f.Result)
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
