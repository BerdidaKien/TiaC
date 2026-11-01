using System;
using System.Linq;
using ASI.Basecode.Data;
using ASI.Basecode.Data.Models;
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
        private readonly TiaCContext _context;

        public AdminController(
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IMapper mapper,
            IUserService userService,
            IAdminUserService adminUserService,
            IAdminReservationService adminReservationService,
            IItemReportService itemReportService,
            TiaCContext context) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            _userService = userService;
            _adminUserService = adminUserService;
            _adminReservationService = adminReservationService;
            _itemReportService = itemReportService;
            _context = context;
        }

        /// <summary>Main Admin Dashboard</summary>
        public IActionResult Index()
        {
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.ActiveStudents = _context.Users.Count(u => u.IsActive && (u.Role == "User" || u.Role == "Student"));
            ViewBag.ActiveCustodians = _context.Users.Count(u => u.IsActive && u.Role == "Custodian");
            ViewBag.PendingReservations = _context.Reservations.Count(r => r.Status == "pending");

            var recentUsers = _context.Users
                .OrderByDescending(u => u.CreatedTime)
                .Take(6)
                .ToList();

            return View(recentUsers);
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

            var adminCode = _session.GetString("UserId") ?? "Admin";
            var result = _adminUserService.CreateUsers(model, adminCode);

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

        // JSON for the item details popup
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

        // Print-ready page: the browser's print dialog saves it as PDF
        [HttpGet]
        public IActionResult ItemReportPrint(ItemReportFilter filter)
        {
            return View(_itemReportService.GetAll(filter));
        }

        /// <summary>System Settings</summary>
        public IActionResult Settings()
        {
            return View();
        }
    }
}