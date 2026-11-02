using System.Collections.Generic;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Services.ServiceModels
{
    public class StudentDashboardViewModel
    {
        // Same data the Custodian dashboard uses (items, categories, filters, counts)
        public DashboardViewModel Catalog { get; set; }

        public int MyActiveBorrowings { get; set; }
        public int MyPendingRequests { get; set; }

        public List<Reservation> RecentReservations { get; set; } = new List<Reservation>();

        // ---- Pagination of the equipment table ----
        public List<EquipmentItem> PagedItems { get; set; } = new List<EquipmentItem>();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalPages { get; set; } = 1;
        public int TotalFiltered { get; set; }
    }
}