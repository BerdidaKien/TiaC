using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Services.ServiceModels
{
    public class AdminReservationFilter
    {
        public string Search { get; set; }
        // pending | approved | awaiting | borrowed | completed | rejected
        public string Status { get; set; }
        public int? CategoryId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Page { get; set; } = 1;
    }

    public class AdminReservationRow
    {
        public int Id { get; set; }

        public string BorrowerFirst { get; set; }
        public string BorrowerLast { get; set; }
        public string BorrowerCode { get; set; }
        public string BorrowerEmail { get; set; }
        public string BorrowerContact { get; set; }

        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string CategoryName { get; set; }
        public string CategoryCode { get; set; }

        public DateTime? ReservationDate { get; set; }   // when the request was submitted
        public DateTime? PickupDate { get; set; }        // requested borrow date
        public DateTime? ReturnDate { get; set; }        // expected return

        public string DbStatus { get; set; }             // raw value saved in the database
        public string Status { get; set; }               // pending | approved | borrowed | overdue | completed | rejected | cancelled

        public string ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewNotes { get; set; }

        public string ReleasedBy { get; set; }
        public DateTime? HandoverTime { get; set; }

        public string ReturnedTo { get; set; }
        public DateTime? ReturnedTime { get; set; }
        public string ReturnCondition { get; set; }
        public bool IsOverdue { get; set; }
        public int DelayHours { get; set; }
        public string DamageNotes { get; set; }

        public string Code => "RES-" + (ReservationDate.HasValue ? ReservationDate.Value.Year.ToString() : "0000") + "-" + Id.ToString("D4");
        public string BorrowerName => ((BorrowerFirst ?? "") + " " + (BorrowerLast ?? "")).Trim();
        public string Initials =>
            ((BorrowerFirst ?? "").Length > 0 ? BorrowerFirst.Substring(0, 1) : "") +
            ((BorrowerLast ?? "").Length > 0 ? BorrowerLast.Substring(0, 1) : "");
    }

    public class AdminReservationsPageViewModel
    {
        public AdminReservationFilter Filter { get; set; } = new AdminReservationFilter();

        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }

        public List<AdminReservationRow> Rows { get; set; } = new List<AdminReservationRow>();
        public List<EquipmentCategory> Categories { get; set; } = new List<EquipmentCategory>();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 5;
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
}