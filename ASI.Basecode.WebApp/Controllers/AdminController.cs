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
using System.Collections.Generic;
using System.IO;
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
        private readonly IUserStatusService _userStatusService;
        private readonly IInfoRequestService _infoRequestService;
        private readonly ProofStorage _proofStorage;

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
            IUserStatusService userStatusService,
            IInfoRequestService infoRequestService,
            ProofStorage proofStorage,
            ICategoryService categoryService) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            _userService = userService;
            _adminUserService = adminUserService;
            _adminReservationService = adminReservationService;
            _itemReportService = itemReportService;
            _adminDashboardService = adminDashboardService;
            _categoryService = categoryService;
            _userStatusService = userStatusService;
            _infoRequestService = infoRequestService;
            _proofStorage = proofStorage;
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
            model ??= new AddUsersPostModel();
            model.UserRows = (model.UserRows ?? new List<AddUserRow>()).Where(r => r != null && !r.IsBlank()).ToList();
            Response.Headers["Cache-Control"] = "no-store";

            if (!model.UserRows.Any())
            {
                model.UserRows.Add(new AddUserRow());
                model.FormError = "Add at least one user.";
                return View(model);
            }

            var result = _adminUserService.CreateUsers(model, AdminCode);
            if (!result.Success)
            {
                // Send everything back so the admin only fixes the flagged fields
                model.FormError = result.GeneralError ?? $"{result.RowErrors.Count} row(s) need attention. Nothing was saved yet.";
                model.RowErrors = result.RowErrors;
                return View(model);
            }

            return View("AddUsersResult", result);
        }
        // ---------- Edit user (account status) ----------
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var model = _userStatusService.GetEditPage(id, AdminCode);
            if (model == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeUserStatus(int id, string status)
        {
            var result = _userStatusService.ChangeStatus(id, status, AdminCode);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(EditUser), new { id });
        }

        // ---------- Information change requests ----------
        [HttpGet]
        public IActionResult InfoRequests(string status, string search, int page = 1)
        {
            return View(_infoRequestService.GetAdminPage(status ?? "pending", search, page));
        }

        [HttpGet]
        public IActionResult ReviewInfoRequest(int id)
        {
            var row = _infoRequestService.GetRequestRow(id);
            if (row == null)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction(nameof(InfoRequests));
            }
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveInfoRequest(int id, Dictionary<string, string> values, string note)
        {
            var result = _infoRequestService.Approve(id, values, note, AdminCode);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

            if (result.Success) return RedirectToAction(nameof(InfoRequests));
            return RedirectToAction(nameof(ReviewInfoRequest), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectInfoRequest(int id, string reason)
        {
            var result = _infoRequestService.Reject(id, reason, AdminCode);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

            if (result.Success) return RedirectToAction(nameof(InfoRequests));
            return RedirectToAction(nameof(ReviewInfoRequest), new { id });
        }

        // Proof photos are only reachable through this admin-only action
        [HttpGet]
        public IActionResult ProofFile(int id)
        {
            var file = _infoRequestService.GetFile(id);
            if (file == null) return NotFound();

            var path = _proofStorage.PhysicalPath(file.StoredName);
            if (path == null || !System.IO.File.Exists(path)) return NotFound();

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Cache-Control"] = "private, no-store";
            return PhysicalFile(path, file.ContentType);
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