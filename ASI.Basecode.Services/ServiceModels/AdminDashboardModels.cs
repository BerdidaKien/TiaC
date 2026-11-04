using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.ServiceModels
{
    public class DashboardUserRow
    {
        public string UserCode { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class DashboardItemRow
    {
        public string ItemCode { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public string Status { get; set; }   // available | borrowed | maintenance
    }

    public class AdminDashboardViewModel
    {
        public int TotalReservations { get; set; }
        public int PendingReservations { get; set; }

        public int TotalItems { get; set; }
        public int AvailableItems { get; set; }
        public int BorrowedItems { get; set; }
        public int MaintenanceItems { get; set; }

        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }

        public int AvailablePercent => TotalItems == 0 ? 0 : (int)Math.Round(AvailableItems * 100.0 / TotalItems);

        public List<DashboardUserRow> RecentUsers { get; set; } = new List<DashboardUserRow>();
        public List<DashboardItemRow> RecentItems { get; set; } = new List<DashboardItemRow>();
        public List<AdminReservationRow> RecentReservations { get; set; } = new List<AdminReservationRow>();
    }
}