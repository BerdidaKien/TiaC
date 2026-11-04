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
    public class ItemReportService : IItemReportService
    {
        private const int PageSize = 10;
        private const int ExportCap = 5000;

        private readonly IEquipmentRepository _equipRepo;
        private readonly IReservationRepository _resRepo;

        public ItemReportService(IEquipmentRepository equipRepo, IReservationRepository resRepo)
        {
            _equipRepo = equipRepo;
            _resRepo = resRepo;
        }

        public ItemReportsPageViewModel GetPage(ItemReportFilter filter) => Build(filter, true);

        public ItemReportsPageViewModel GetAll(ItemReportFilter filter) => Build(filter, false);

        private ItemReportsPageViewModel Build(ItemReportFilter filter, bool paged)
        {
            var f = filter ?? new ItemReportFilter();
            var now = DateTime.Now;

            // Loans that are out right now (a short list: at most one per item)
            var open = _resRepo.GetTransactions().Where(t => t.ActualReturnTime == null).ToList();
            var openByItem = open.GroupBy(t => t.EquipmentItemId)
                                 .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.ActualPickupTime).First());
            var openIds = openByItem.Keys.ToList();

            var q = _equipRepo.GetEquipmentItems()   // not archived, includes Category
                  .Where(i => !ItemStatuses.AwaitingCategory.Contains(i.Status));

            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var term = f.Search.Trim();
                var borrowerItemIds = open.Where(t => MatchesBorrower(t.Borrower, term))
                                          .Select(t => t.EquipmentItemId)
                                          .Distinct()
                                          .ToList();

                q = q.Where(i => i.ItemCode.Contains(term)
                              || i.BrandModel.Contains(term)
                              || i.SerialNumber.Contains(term)
                              || borrowerItemIds.Contains(i.Id));
            }

            if (f.CategoryId.HasValue)
            {
                q = q.Where(i => i.CategoryId == f.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(f.Condition))
            {
                q = q.Where(i => i.ConditionRating == f.Condition);
            }

            // Card counts (search, category and condition apply; the status filter does not)
            var total = q.Count();
            var maintenance = q.Count(i => i.Status == "maintenance");
            var borrowed = q.Count(i => i.Status != "maintenance" && (i.Status == "borrowed" || openIds.Contains(i.Id)));
            var available = total - maintenance - borrowed;

            switch (f.Status)
            {
                case "maintenance":
                    q = q.Where(i => i.Status == "maintenance");
                    break;
                case "borrowed":
                    q = q.Where(i => i.Status != "maintenance" && (i.Status == "borrowed" || openIds.Contains(i.Id)));
                    break;
                case "available":
                    q = q.Where(i => i.Status != "maintenance" && i.Status != "borrowed" && !openIds.Contains(i.Id));
                    break;
            }

            var totalRows = q.Count();
            var pageSize = paged ? PageSize : Math.Min(Math.Max(totalRows, 1), ExportCap);
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalRows / (double)pageSize));
            var page = paged ? Math.Min(Math.Max(1, f.Page), totalPages) : 1;

            var items = q.OrderBy(i => i.ItemCode)
                         .Skip((page - 1) * pageSize)
                         .Take(pageSize)
                         .ToList();

            var rows = items.Select(i =>
            {
                openByItem.TryGetValue(i.Id, out var loan);

                var status = i.Status == "maintenance" ? "maintenance"
                           : (i.Status == "borrowed" || loan != null) ? "borrowed"
                           : "available";

                var due = loan?.Reservation?.EstimatedReturnDate;

                return new ItemReportRow
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ItemName = i.BrandModel,
                    CategoryName = i.Category?.CategoryName ?? "General",
                    CategoryCode = i.Category?.CategoryCode,
                    SerialNumber = i.SerialNumber,
                    StorageLocation = i.StorageLocation,
                    Status = status,
                    Condition = i.ConditionRating,
                    BorrowerName = loan == null ? null : (loan.Borrower.FirstName + " " + loan.Borrower.LastName).Trim(),
                    BorrowerCode = loan?.Borrower.UserId,
                    BorrowDate = loan?.ActualPickupTime,
                    ExpectedReturn = due,
                    IsOverdue = loan != null && due.HasValue && due.Value < now
                };
            }).ToList();

            return new ItemReportsPageViewModel
            {
                Filter = f,
                TotalItems = total,
                AvailableCount = available,
                BorrowedCount = borrowed,
                MaintenanceCount = maintenance,
                Rows = rows,
                Categories = _resRepo.GetCategories().OrderBy(c => c.CategoryName).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalRows = totalRows,
                TotalPages = totalPages
            };
        }

        public ItemDetailsDto GetItemDetails(int id)
        {
            var item = _equipRepo.GetEquipmentById(id);
            if (item == null) return null;

            var all = _resRepo.GetTransactions().Where(t => t.EquipmentItemId == id);
            var totalBorrows = all.Count();
            var recent = all.OrderByDescending(t => t.ActualPickupTime).Take(8).ToList();

            var current = recent.FirstOrDefault(t => t.ActualReturnTime == null);

            var statusKey = item.Status == "maintenance" ? "maintenance"
                          : (item.Status == "borrowed" || current != null) ? "borrowed"
                          : "available";

            return new ItemDetailsDto
            {
                ItemCode = item.ItemCode,
                Name = item.BrandModel,
                Category = item.Category?.CategoryName ?? "General",
                SerialNumber = item.SerialNumber,
                StorageLocation = item.StorageLocation,
                StatusKey = statusKey,
                StatusText = ItemLabels.Status(statusKey),
                ConditionText = ItemLabels.Condition(item.ConditionRating),
                AddedOn = Fmt(item.CreatedTime),
                TotalBorrows = totalBorrows,
                Current = current == null ? null : ToLoan(current),
                History = recent.Where(t => t.ActualReturnTime != null).Select(ToLoan).ToList()
            };
        }

        public byte[] ExportCsv(ItemReportFilter filter)
        {
            var rows = GetAll(filter).Rows;

            var sb = new StringBuilder();
            sb.AppendLine("Item Code,Item Name,Category,Serial Number,Storage Location,Status,Condition,"
                        + "Borrower ID,Borrower Name,Borrow Date,Expected Return,Overdue");

            foreach (var r in rows)
            {
                var cells = new[]
                {
                    r.ItemCode,
                    r.ItemName,
                    r.CategoryName,
                    r.SerialNumber,
                    r.StorageLocation,
                    ItemLabels.Status(r.Status),
                    ItemLabels.Condition(r.Condition),
                    r.BorrowerCode,
                    r.BorrowerName,
                    r.BorrowDate.HasValue ? r.BorrowDate.Value.ToString("yyyy-MM-dd HH:mm") : "",
                    r.ExpectedReturn.HasValue ? r.ExpectedReturn.Value.ToString("yyyy-MM-dd HH:mm") : "",
                    r.IsOverdue ? "Yes" : "No"
                };
                sb.AppendLine(string.Join(",", cells.Select(Csv)));
            }

            // UTF-8 with BOM so Excel opens it correctly
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        // ---------- helpers ----------
        private static bool MatchesBorrower(User u, string term)
        {
            if (u == null) return false;
            return Has(u.UserId, term) || Has(u.FirstName + " " + u.LastName, term);
        }

        private static bool Has(string text, string term)
        {
            return text != null && text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Fmt(DateTime? d)
        {
            return d.HasValue ? d.Value.ToString("MMM d, yyyy h:mm tt") : null;
        }

        private static ItemLoanDto ToLoan(BorrowingTransaction t)
        {
            var due = t.Reservation?.EstimatedReturnDate;
            return new ItemLoanDto
            {
                Borrower = (t.Borrower.FirstName + " " + t.Borrower.LastName).Trim(),
                BorrowerCode = t.Borrower.UserId,
                Handover = Fmt(t.ActualPickupTime),
                Due = Fmt(due),
                Returned = Fmt(t.ActualReturnTime),
                ReturnCondition = t.ActualReturnTime == null ? null : ItemLabels.Condition(t.ReturnCondition),
                LateHours = t.IsOverdue ? t.DelayDurationHours : 0,
                OverdueNow = t.ActualReturnTime == null && due.HasValue && due.Value < DateTime.Now,
                Notes = t.DamageNotes,
                ReleasedBy = t.ReleasedByNavigation == null
                    ? null
                    : (t.ReleasedByNavigation.FirstName + " " + t.ReleasedByNavigation.LastName).Trim()
            };
        }

        private static string Csv(string value)
        {
            value = value ?? "";

            // Stop spreadsheet apps from running cells that start like a formula
            if (value.Length > 0 && "=+-@".IndexOf(value[0]) >= 0)
            {
                value = "'" + value;
            }

            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            {
                value = "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}