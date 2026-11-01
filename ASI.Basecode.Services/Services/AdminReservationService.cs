using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Services
{
    public class AdminReservationService : IAdminReservationService
    {
        private const int PageSize = 5;
        private readonly IReservationRepository _repo;

        public AdminReservationService(IReservationRepository repo)
        {
            _repo = repo;
        }

        public AdminReservationsPageViewModel GetPage(AdminReservationFilter filter)
        {
            var f = filter ?? new AdminReservationFilter();
            var now = DateTime.Now;

            // Search, category and date filters (these also drive the four cards)
            var baseQ = _repo.GetReservations();

            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var term = f.Search.Trim();

                // Lets the admin paste a code like RES-2026-0012
                int? idFromCode = null;
                var m = Regex.Match(term, @"^RES-\d{4}-(\d+)$", RegexOptions.IgnoreCase);
                if (m.Success && int.TryParse(m.Groups[1].Value, out var n)) idFromCode = n;
                var codeId = idFromCode ?? -1;

                baseQ = baseQ.Where(r =>
                    r.Id == codeId ||
                    r.Borrower.UserId.Contains(term) ||
                    (r.Borrower.FirstName + " " + r.Borrower.LastName).Contains(term) ||
                    r.EquipmentItem.ItemCode.Contains(term) ||
                    r.EquipmentItem.BrandModel.Contains(term));
            }

            if (f.CategoryId.HasValue)
            {
                baseQ = baseQ.Where(r => r.EquipmentItem.CategoryId == f.CategoryId.Value);
            }

            if (f.DateFrom.HasValue)
            {
                var from = f.DateFrom.Value.Date;
                baseQ = baseQ.Where(r => r.CreatedTime >= from);
            }

            if (f.DateTo.HasValue)
            {
                var to = f.DateTo.Value.Date.AddDays(1);
                baseQ = baseQ.Where(r => r.CreatedTime < to);
            }

            // Card counts in one query
            var byStatus = baseQ.GroupBy(r => r.Status)
                                .Select(g => new { Status = g.Key, Count = g.Count() })
                                .ToList();
            int CountOf(string s) => byStatus.Where(x => x.Status == s).Sum(x => x.Count);

            // Status filter + paging apply to the table only
            var filtered = ApplyStatus(baseQ, f.Status);
            var totalRows = filtered.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalRows / (double)PageSize));
            var page = Math.Min(Math.Max(1, f.Page), totalPages);

            var rows = filtered
                .OrderByDescending(r => r.CreatedTime).ThenByDescending(r => r.Id)
                .Skip((page - 1) * PageSize).Take(PageSize)
                .Select(r => new AdminReservationRow
                {
                    Id = r.Id,
                    BorrowerFirst = r.Borrower.FirstName,
                    BorrowerLast = r.Borrower.LastName,
                    BorrowerCode = r.Borrower.UserId,
                    BorrowerEmail = r.Borrower.Email,
                    BorrowerContact = r.Borrower.ContactNumber,
                    ItemName = r.EquipmentItem.BrandModel,
                    ItemCode = r.EquipmentItem.ItemCode,
                    CategoryName = r.EquipmentItem.Category.CategoryName,
                    CategoryCode = r.EquipmentItem.Category.CategoryCode,
                    ReservationDate = r.CreatedTime,
                    PickupDate = r.RequestedPickupDate,
                    ReturnDate = r.EstimatedReturnDate,
                    DbStatus = r.Status,
                    ReviewedBy = r.ReviewedByNavigation == null
                        ? null
                        : r.ReviewedByNavigation.FirstName + " " + r.ReviewedByNavigation.LastName,
                    ReviewedAt = r.ReviewedAt,
                    ReviewNotes = r.ReviewNotes,
                    HandoverTime = r.BorrowingTransaction == null
                        ? (DateTime?)null
                        : r.BorrowingTransaction.ActualPickupTime,
                    ReleasedBy = (r.BorrowingTransaction == null || r.BorrowingTransaction.ReleasedByNavigation == null)
                        ? null
                        : r.BorrowingTransaction.ReleasedByNavigation.FirstName + " " + r.BorrowingTransaction.ReleasedByNavigation.LastName,
                    ReturnedTime = r.BorrowingTransaction == null
                        ? (DateTime?)null
                        : r.BorrowingTransaction.ActualReturnTime,
                    ReturnedTo = (r.BorrowingTransaction == null || r.BorrowingTransaction.ReturnedToNavigation == null)
                        ? null
                        : r.BorrowingTransaction.ReturnedToNavigation.FirstName + " " + r.BorrowingTransaction.ReturnedToNavigation.LastName,
                    ReturnCondition = r.BorrowingTransaction == null ? null : r.BorrowingTransaction.ReturnCondition,
                    IsOverdue = r.BorrowingTransaction != null && r.BorrowingTransaction.IsOverdue,
                    DelayHours = r.BorrowingTransaction == null ? 0 : r.BorrowingTransaction.DelayDurationHours,
                    DamageNotes = r.BorrowingTransaction == null ? null : r.BorrowingTransaction.DamageNotes
                })
                .ToList();

            foreach (var row in rows)
            {
                row.Status = Lifecycle(row, now);
            }

            return new AdminReservationsPageViewModel
            {
                Filter = f,
                TotalCount = byStatus.Sum(x => x.Count),
                PendingCount = CountOf("pending"),
                ApprovedCount = CountOf("approved"),
                RejectedCount = CountOf("declined"),
                Rows = rows,
                Categories = _repo.GetCategories().OrderBy(c => c.CategoryName).ToList(),
                Page = page,
                PageSize = PageSize,
                TotalRows = totalRows,
                TotalPages = totalPages
            };
        }

        private static IQueryable<Reservation> ApplyStatus(IQueryable<Reservation> q, string status)
        {
            switch (status)
            {
                case "pending":
                    return q.Where(r => r.Status == "pending");
                case "approved":   // everything the custodian approved, at any stage
                    return q.Where(r => r.Status == "approved");
                case "awaiting":   // approved, not handed over yet
                    return q.Where(r => r.Status == "approved" && r.BorrowingTransaction == null);
                case "borrowed":   // handed over, not returned (includes overdue)
                    return q.Where(r => r.Status == "approved" && r.BorrowingTransaction != null
                                     && r.BorrowingTransaction.ActualReturnTime == null);
                case "completed":  // returned
                    return q.Where(r => r.Status == "approved" && r.BorrowingTransaction != null
                                     && r.BorrowingTransaction.ActualReturnTime != null);
                case "rejected":
                    return q.Where(r => r.Status == "declined");
                default:
                    return q;
            }
        }

        // What the badge in the table shows
        private static string Lifecycle(AdminReservationRow r, DateTime now)
        {
            switch (r.DbStatus)
            {
                case "pending":
                    return "pending";
                case "declined":
                    return "rejected";
                case "approved":
                    if (!r.HandoverTime.HasValue) return "approved";
                    if (r.ReturnedTime.HasValue) return "completed";
                    return (r.ReturnDate.HasValue && r.ReturnDate.Value < now) ? "overdue" : "borrowed";
                default:
                    return r.DbStatus ?? "unknown";
            }
        }
    }
}
