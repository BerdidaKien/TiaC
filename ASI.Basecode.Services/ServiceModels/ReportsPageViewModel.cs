using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Services.ServiceModels
{
    public class TopItemRow
    {
        public string ItemCode { get; set; }
        public string BrandModel { get; set; }
        public string CategoryName { get; set; }
        public int Requests { get; set; }
    }

    public class CategoryUsageRow
    {
        public string CategoryName { get; set; }
        public int Borrows { get; set; }
        public int Percent { get; set; }
    }

    public class CalendarItemOption
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Label { get; set; }
    }

    // Sent to the calendar as JSON
    public class CalendarEventDto
    {
        public string Id { get; set; }
        public int ItemId { get; set; }
        public string ItemCode { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public string Borrower { get; set; }
        public string BorrowerCode { get; set; }
        public string Status { get; set; }   // pending | approved | borrowed | overdue | returned
        public string Start { get; set; }    // yyyy-MM-ddTHH:mm (local time)
        public string End { get; set; }
    }

    public class CalendarDataDto
    {
        public int TotalItems { get; set; }
        public List<CalendarEventDto> Events { get; set; } = new List<CalendarEventDto>();
    }

    public class ReportsPageViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public int TotalTransactions { get; set; }
        public int OverdueFlagged { get; set; }
        public int TotalDelayHours { get; set; }
        public int DamagedLogs { get; set; }
        public int UtilizationRate { get; set; }
        public int TotalItems { get; set; }

        public List<TopItemRow> TopItems { get; set; } = new List<TopItemRow>();
        public List<CategoryUsageRow> CategoryUsage { get; set; } = new List<CategoryUsageRow>();

        // Calendar filters
        public List<EquipmentCategory> Categories { get; set; } = new List<EquipmentCategory>();
        public List<CalendarItemOption> ItemOptions { get; set; } = new List<CalendarItemOption>();
    }
}