using System;
using System.Linq;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ASI.Basecode.WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase<AdminController>
    {
        private readonly IUserService _userService;
        private readonly IAdminUserService _adminUserService;
        private readonly IAdminReservationService _adminReservationService;
        private readonly IItemReportService _itemReportService;
        private readonly IAdminDashboardService _adminDashboardService;
        private readonly ICategoryService _categoryService;

        public AdminController(
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IMapper mapper,
            IUserService userService,
            IAdminUserService adminUserService,
            IAdminReservationService adminReservationService,
            IItemReportService itemReportService,
            IAdminDashboardService adminDashboardService,
            ICategoryService categoryService) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            _userService = userService;
            _adminUserService = adminUserService;
            _adminReservationService = adminReservationService;
            _itemReportService = itemReportService;
            _adminDashboardService = adminDashboardService;
            _categoryService = categoryService;
        }

        private string AdminCode => _session.GetString("UserId") ?? "Admin";

        /// <summary>Main Admin Dashboard</summary>
        public IActionResult Index()
        {
            ViewBag.PendingCategories = _categoryService.PendingCount();
            return View(_adminDashboardService.GetDashboard());
        }

        /// <summary>User Management Overview Screen</summary>
        [HttpGet]
        public IActionResult Users(string search, string role, string status)
        {
            var model = _userService.GetUserManagementData(search, role, status);
            return View(model);
        }

        /// <summary>Batch Account Creation Form</summary>
        [HttpGet]
        public IActionResult AddUsers()
        {
            return View(new AddUsersPostModel());
        }

        /// <summary>Batch creation; shows a one-time results page with temporary passwords</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUsers(AddUsersPostModel model)
        {
            if (model == null || model.UserRows == null || !model.UserRows.Any())
            {
                return View(new AddUsersPostModel());
            }

            var result = _adminUserService.CreateUsers(model, AdminCode);

            Response.Headers["Cache-Control"] = "no-store";
            return View("AddUsersResult", result);
        }

        /// <summary>System-wide reservations oversight (read-only)</summary>
        [HttpGet]
        public IActionResult Reservations(AdminReservationFilter filter)
        {
            return View(_adminReservationService.GetPage(filter));
        }

        // ---------- Item Reports ----------
        [HttpGet]
        public IActionResult Reports(ItemReportFilter filter)
        {
            return View(_itemReportService.GetPage(filter));
        }

        [HttpGet]
        public IActionResult ItemDetails(int id)
        {
            var details = _itemReportService.GetItemDetails(id);
            if (details == null) return NotFound();
            return Json(details);
        }

        [HttpGet]
        public IActionResult ExportItemsCsv(ItemReportFilter filter)
        {
            var bytes = _itemReportService.ExportCsv(filter);
            return File(bytes, "text/csv", $"item-report-{DateTime.Now:yyyyMMdd-HHmm}.csv");
        }

        [HttpGet]
        public IActionResult ItemReportPrint(ItemReportFilter filter)
        {
            return View(_itemReportService.GetAll(filter));
        }

        // ---------- Categories ----------
        [HttpGet]
        public IActionResult Categories()
        {
            return View(_categoryService.GetPage());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCategory(string name, string description)
            => CategoryResult(_categoryService.Create(name, description, AdminCode));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCategory(int id, string name, string description)
            => CategoryResult(_categoryService.Update(id, name, description, AdminCode));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleCategory(int id, bool active)
            => CategoryResult(_categoryService.SetActive(id, active, AdminCode));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategory(int id)
            => CategoryResult(_categoryService.Delete(id));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveCategoryRequest(int itemId, string categoryName)
            => CategoryResult(_categoryService.ApproveRequest(itemId, categoryName, AdminCode));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectCategoryRequest(int itemId, string reason)
            => CategoryResult(_categoryService.RejectRequest(itemId, reason, AdminCode));

        private IActionResult CategoryResult(ServiceResult result)
        {
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Categories));
        }

        /// <summary>System Settings</summary>
        public IActionResult Settings()
        {
            return View();
        }
    }
}