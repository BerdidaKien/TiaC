using System.Collections.Generic;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Services.ServiceModels
{
    public class DashboardViewModel
    {
        public int TotalEquipment { get; set; }
        public int AvailableItems { get; set; }
        public int CurrentlyBorrowed { get; set; }
        public int PendingRequests { get; set; }

        public List<EquipmentItem> Items { get; set; } = new List<EquipmentItem>();
        public List<EquipmentCategory> Categories { get; set; } = new List<EquipmentCategory>();
        public List<int> BorrowedItemIds { get; set; } = new List<int>();

        // Current filter values, so the form keeps what the user picked
        public string Search { get; set; }
        public int? CategoryId { get; set; }
        public string Status { get; set; }
    }
}