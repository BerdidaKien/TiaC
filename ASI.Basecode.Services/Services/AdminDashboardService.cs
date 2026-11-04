using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;

namespace ASI.Basecode.Services.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IUserAdminRepository _userRepo;
        private readonly IEquipmentRepository _equipRepo;
        private readonly IReservationRepository _resRepo;
        private readonly IAdminReservationService _reservations;
        private readonly IItemReportService _items;

        public AdminDashboardService(
            IUserAdminRepository userRepo,
            IEquipmentRepository equipRepo,
            IReservationRepository resRepo,
            IAdminReservationService reservations,
            IItemReportService items)
        {
            _userRepo = userRepo;
            _equipRepo = equipRepo;
            _resRepo = resRepo;
            _reservations = reservations;
            _items = items;
        }

        public AdminDashboardViewModel GetDashboard()
        {
            // Reuse the Reservations and Item Reports logic so every page shows the same numbers.
            // The first page of reservations is already the 5 newest.
            var res = _reservations.GetPage(new AdminReservationFilter());
            var items = _items.GetPage(new ItemReportFilter());

            var users = _userRepo.GetUsers();

            var recentUsers = users
                .OrderByDescending(u => u.CreatedTime).ThenByDescending(u => u.Id)
                .Take(5)
                .Select(u => new DashboardUserRow
                {
                    UserCode = u.UserId,
                    Name = u.FirstName + " " + u.LastName,
                    Role = u.Role,
                    IsActive = u.IsActive
                })
                .ToList();

            var newestItems = _equipRepo.GetEquipmentItems()   // not archived, includes Category
                .Where(i => !ItemStatuses.AwaitingCategory.Contains(i.Status))
                .OrderByDescending(i => i.CreatedTime).ThenByDescending(i => i.Id)
                .Take(5)
                .ToList();

            var ids = newestItems.Select(i => i.Id).ToList();
            var openIds = _resRepo.GetTransactions()
                .Where(t => t.ActualReturnTime == null && ids.Contains(t.EquipmentItemId))
                .Select(t => t.EquipmentItemId)
                .Distinct()
                .ToList();

            var recentItems = newestItems.Select(i => new DashboardItemRow
            {
                ItemCode = i.ItemCode,
                Name = i.BrandModel,
                CategoryName = i.Category?.CategoryName ?? "General",
                Status = i.Status == "maintenance" ? "maintenance"
                       : (i.Status == "borrowed" || openIds.Contains(i.Id)) ? "borrowed"
                       : "available"
            }).ToList();

            return new AdminDashboardViewModel
            {
                TotalReservations = res.TotalCount,
                PendingReservations = res.PendingCount,

                TotalItems = items.TotalItems,
                AvailableItems = items.AvailableCount,
                BorrowedItems = items.BorrowedCount,
                MaintenanceItems = items.MaintenanceCount,

                TotalUsers = users.Count(),
                ActiveUsers = users.Count(u => u.IsActive),

                RecentUsers = recentUsers,
                RecentItems = recentItems,
                RecentReservations = res.Rows
            };
        }
    }
}