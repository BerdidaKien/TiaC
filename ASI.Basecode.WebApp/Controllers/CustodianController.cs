using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.Services.Services;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ASI.Basecode.WebApp.Controllers
{
    [Authorize(Roles = "Custodian")]
    public class CustodianController : ControllerBase<CustodianController>
    {
        private readonly IEquipmentService _equipmentService;
        private readonly IReservationService _reservationService;
        private readonly IHistoryService _historyService;
        private readonly IReportService _reportService;
        private readonly IUserService _userService; // Added field
        private readonly IWebHostEnvironment _env;
        private readonly ISettingsService _settingsService;

        public CustodianController(
            IEquipmentService equipmentService,
            IReservationService reservationService,
            IHistoryService historyService,
            IReportService reportService,
            ISettingsService settingsService,
            IUserService userService, // Injected dependency
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IMapper mapper)
            : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            _equipmentService = equipmentService;
            _reservationService = reservationService;
            _historyService = historyService;
            _reportService = reportService;
            _userService = userService;
            _settingsService = settingsService;// Assigned field
            _env = env;
        }

        // ---------- Dashboard ----------
        public IActionResult Index(string search, int? categoryId, string status)
        {
            var model = _equipmentService.GetDashboardSummary(search, categoryId, status);
            return View(model);
        }

        // ---------- Equipment ----------
        public IActionResult Equipment(int page = 1)
        {
            return EquipmentPage(page);
        }

        private const int EquipmentPageSize = 5;

        private IActionResult EquipmentPage(int page)
        {
            var all = _equipmentService.GetAllEquipment()
                .OrderByDescending(i => i.CreatedTime)
                .ThenByDescending(i => i.Id)
                .ToList();

            var totalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)EquipmentPageSize));
            page = Math.Clamp(page, 1, totalPages);

            ViewBag.Page = page;
            ViewBag.PageSize = EquipmentPageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = all.Count;

            PopulateCategoryList();
            return View("Equipment", all.Skip((page - 1) * EquipmentPageSize).Take(EquipmentPageSize).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddEquipment(EquipmentItem model, IFormFile photo)
        {
            model.CreatedBy = CurrentUserCode ?? "Custodian";
            model.UpdatedBy = model.CreatedBy;
            model.CreatedTime = DateTime.Now;
            model.UpdatedTime = DateTime.Now;

            if (model.CategoryId == 0) model.CategoryId = null;

            var photoError = ValidatePhoto(photo);
            if (photoError != null)
            {
                TempData["ErrorMessage"] = photoError;
                return RedirectToAction(nameof(Equipment));
            }

            var imagePath = SavePhoto(photo);
            model.ImagePath = imagePath;

            var result = _equipmentService.AddEquipment(model);
            if (!result.Success)
            {
                DeletePhotoFile(imagePath);
            }

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Equipment));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditEquipment(EquipmentItem model, IFormFile photo, int page = 1)
        {
            var photoError = ValidatePhoto(photo);
            if (photoError != null)
            {
                TempData["ErrorMessage"] = photoError;
                return RedirectToAction(nameof(Equipment), new { page });
            }

            if (model.CategoryId == 0) model.CategoryId = null;

            var oldImagePath = _equipmentService.GetEquipmentById(model.Id)?.ImagePath;
            string newImagePath = null;

            model.ImagePath = null;
            if (photo != null && photo.Length > 0)
            {
                newImagePath = SavePhoto(photo);
                model.ImagePath = newImagePath;
            }

            model.UpdatedBy = CurrentUserCode ?? "Custodian";
            var result = _equipmentService.UpdateEquipment(model);

            if (result.Success)
            {
                if (newImagePath != null) DeletePhotoFile(oldImagePath);
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                DeletePhotoFile(newImagePath);
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Equipment), new { page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ArchiveEquipment(int id, int page = 1)
        {
            var result = _equipmentService.ArchiveEquipment(id, User.Identity?.Name ?? "Custodian");
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Equipment), new { page });
        }

        // ---------- Photo upload helpers ----------
        private const long MaxPhotoBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        private static string ValidatePhoto(IFormFile photo)
        {
            if (photo == null || photo.Length == 0) return null;

            var ext = Path.GetExtension(photo.FileName ?? "").ToLowerInvariant();
            if (!AllowedPhotoExtensions.Contains(ext) ||
                !(photo.ContentType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return "Please upload a JPG, PNG or WEBP image.";
            }

            if (photo.Length > MaxPhotoBytes)
            {
                return "The photo must be 5 MB or smaller.";
            }

            return null;
        }

        private string SavePhoto(IFormFile photo)
        {
            if (photo == null || photo.Length == 0) return null;

            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var folder = Path.Combine(_env.WebRootPath, "uploads", "equipment");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
            {
                photo.CopyTo(stream);
            }

            return "/uploads/equipment/" + fileName;
        }

        private void DeletePhotoFile(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) ||
                !imagePath.StartsWith("/uploads/equipment/", StringComparison.OrdinalIgnoreCase))
                return;

            var fullPath = Path.Combine(_env.WebRootPath, "uploads", "equipment", Path.GetFileName(imagePath));
            try
            {
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }
            catch (IOException) { }
        }

        private void PopulateCategoryList()
        {
            var all = _equipmentService.GetCategories().OrderBy(c => c.CategoryName).ToList();

            ViewBag.CategoryList = all
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.CategoryName })
                .ToList();

            ViewBag.EditCategoryList = all
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.IsActive ? c.CategoryName : c.CategoryName + " (disabled)"
                })
                .ToList();
        }

        // ---------- Reservations ----------
        public IActionResult Reservations(string search, int? categoryId)
        {
            var model = _reservationService.GetPage(search, categoryId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveRequest(int id, string reviewNotes)
            => HandleResult(_reservationService.Approve(id, reviewNotes, CurrentUserCode));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeclineRequest(int id, string declineReason)
            => HandleResult(_reservationService.Decline(id, declineReason, CurrentUserCode));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReleaseItem(int id)
            => HandleResult(_reservationService.Release(id, CurrentUserCode));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReturnItem(int id, string returnCondition, string damageNotes)
            => HandleResult(_reservationService.Return(id, returnCondition, damageNotes, CurrentUserCode));

        private string CurrentUserCode => _session.GetString("UserId");

        private IActionResult HandleResult(ServiceResult result)
        {
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Reservations));
        }

        // ---------- History ----------
        public IActionResult History(HistoryFilter filter)
        {
            return View(_historyService.GetPage(filter));
        }

        public IActionResult ExportHistoryCsv(HistoryFilter filter)
        {
            var bytes = _historyService.ExportCsv(filter);
            return File(bytes, "text/csv", $"borrowing-history-{DateTime.Now:yyyyMMdd-HHmm}.csv");
        }

        public IActionResult HistoryPrint(HistoryFilter filter)
        {
            return View(_historyService.GetPage(filter));
        }

        // ---------- Reports ----------
        public IActionResult Reports(DateTime? from, DateTime? to)
        {
            return View(_reportService.GetPage(from, to));
        }

        [HttpGet]
        public IActionResult CalendarData(DateTime start, DateTime end, int? categoryId, int? itemId)
        {
            if (end <= start || (end - start).TotalDays > 62)
            {
                return BadRequest("Invalid date range.");
            }
            return Json(_reportService.GetCalendar(start, end, categoryId, itemId));
        }

        // ---------- Other pages ----------
        [HttpGet]
        public IActionResult Profile()
        {
            // Try session first, then fallback to authentication claims
            var userIdStr = HttpContext.Session.GetString("UserId")
                         ?? User.Identity?.Name
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Account");
            }

            var model = _userService.GetCustodianProfile(userIdStr);
            if (model == null)
            {
                // Debug fallback: If lookup by string user_id fails, try getting the first Custodian user
                var fallbackUser = _userService.GetUserManagementData(null, "Custodian", null).Users.FirstOrDefault();
                if (fallbackUser != null)
                {
                    model = _userService.GetCustodianProfile(fallbackUser.UserId);
                }
            }

            if (model == null)
            {
                return NotFound("Custodian profile not found.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(CustodianProfileViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
            {
                TempData["ErrorMessage"] = "First name and Last name cannot be empty.";
                return RedirectToAction("Profile");
            }

            bool updated = _userService.UpdateCustodianProfile(model.Id, model.FirstName, model.LastName, model.ContactNumber);

            if (updated)
            {
                TempData["SuccessMessage"] = "Profile details updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update profile. Please try again.";
            }

            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult Settings()
        {
            return View(_settingsService.Get());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveSettings(BorrowingSettings model)
        {
            var result = _settingsService.Save(model, CurrentUserCode ?? "Custodian");

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Settings));
            }

            // Keep what the custodian typed, and show why it was refused
            ViewBag.Error = result.Message;
            return View("Settings", model ?? _settingsService.Get());
        }
    }
}