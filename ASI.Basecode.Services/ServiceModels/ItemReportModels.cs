using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Services.ServiceModels
{
    public static class ItemLabels
    {
        public static string Condition(string c)
        {
            switch (c)
            {
                case "excellent": return "Excellent";
                case "good": return "Good";
                case "minor_wear": return "Minor Wear";
                case "needs_maintenance": return "Needs Maintenance";
                case null:
                case "": return "-";
                default: return c;
            }
        }

        public static string Status(string s)
        {
            switch (s)
            {
                case "available": return "Available";
                case "borrowed": return "Borrowed";
                case "maintenance": return "For Maintenance";
                default: return s ?? "-";
            }
        }
    }

    public class ItemReportFilter
    {
        public string Search { get; set; }
        public string Status { get; set; }      // available | borrowed | maintenance
        public string Condition { get; set; }   // excellent | good | minor_wear | needs_maintenance
        public int? CategoryId { get; set; }
        public int Page { get; set; } = 1;
    }

    public class ItemReportRow
    {
        public int Id { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string CategoryName { get; set; }
        public string CategoryCode { get; set; }
        public string SerialNumber { get; set; }
        public string StorageLocation { get; set; }
        public string Status { get; set; }        // available | borrowed | maintenance
        public string Condition { get; set; }

        public string BorrowerName { get; set; }
        public string BorrowerCode { get; set; }
        public DateTime? BorrowDate { get; set; }
        public DateTime? ExpectedReturn { get; set; }
        public bool IsOverdue { get; set; }

        public string BorrowerInitials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(BorrowerName)) return "";
                var parts = BorrowerName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Length == 1
                    ? parts[0].Substring(0, 1)
                    : parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1);
            }
        }
    }

    public class ItemReportsPageViewModel
    {
        public ItemReportFilter Filter { get; set; } = new ItemReportFilter();

        public int TotalItems { get; set; }
        public int AvailableCount { get; set; }
        public int BorrowedCount { get; set; }
        public int MaintenanceCount { get; set; }

        public List<ItemReportRow> Rows { get; set; } = new List<ItemReportRow>();
        public List<EquipmentCategory> Categories { get; set; } = new List<EquipmentCategory>();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRows { get; set; }
        public int TotalPages { get; set; } = 1;

        public int FirstShown => TotalRows == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int LastShown => Math.Min(Page * PageSize, TotalRows);

        // Page numbers to show; 0 means "..."
        public List<int> PageWindow()
        {
            var pages = new SortedSet<int> { 1, TotalPages, Page - 1, Page, Page + 1 };
            var result = new List<int>();
            int prev = 0;
            foreach (var p in pages.Where(p => p >= 1 && p <= TotalPages))
            {
                if (prev != 0 && p - prev > 1) result.Add(0);
                result.Add(p);
                prev = p;
            }
            return result;
        }
    }

    // ---------- Item detail popup (sent as JSON) ----------
    public class ItemLoanDto
    {
        public string Borrower { get; set; }
        public string BorrowerCode { get; set; }
        public string Handover { get; set; }
        public string Due { get; set; }
        public string Returned { get; set; }
        public string ReturnCondition { get; set; }
        public int LateHours { get; set; }
        public bool OverdueNow { get; set; }
        public string Notes { get; set; }
        public string ReleasedBy { get; set; }
    }

    public class ItemDetailsDto
    {
        public string ItemCode { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string SerialNumber { get; set; }
        public string StorageLocation { get; set; }
        public string StatusKey { get; set; }
        public string StatusText { get; set; }
        public string ConditionText { get; set; }
        public string AddedOn { get; set; }
        public int TotalBorrows { get; set; }
        public ItemLoanDto Current { get; set; }
        public List<ItemLoanDto> History { get; set; } = new List<ItemLoanDto>();
    }
}