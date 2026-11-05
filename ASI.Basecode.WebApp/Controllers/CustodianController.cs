using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
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
        private readonly ISettingsService _settingsService;
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _env;
        private readonly ProfilePhotoStorage _profilePhotos;

        public CustodianController(
            IEquipmentService equipmentService,
            IReservationService reservationService,
            IHistoryService historyService,
            IReportService reportService,
            ISettingsService settingsService,
            IUserService userService,
            ProfilePhotoStorage profilePhotos,
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
            _settingsService = settingsService;
            _userService = userService;
            _profilePhotos = profilePhotos;
            _env = env;
        }

        private string CurrentUserCode => _session.GetString("UserId");

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
            var result = _equipmentService.ArchiveEquipment(id, CurrentUserCode ?? "Custodian");
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Equipment), new { page });
        }

        // ---------- Equipment photo helpers ----------
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

        // ---------- Profile ----------
        [HttpGet]
        public IActionResult Profile()
        {
            var model = _userService.GetCustodianProfile(CurrentUserCode);
            if (model == null) return RedirectToAction("Login", "Account");
            return View(model);
        }

        // Custodians can only change their contact number and photo here.
        // Name, ID and email go through an information change request.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(string contactNumber, IFormFile photo)
        {
            string newPhoto = null;

            if (photo != null && photo.Length > 0)
            {
                if (!_profilePhotos.TrySave(photo, out newPhoto, out var photoError))
                {
                    TempData["ErrorMessage"] = photoError;
                    return RedirectToAction(nameof(Profile));
                }
            }

            var oldPhoto = _userService.GetProfileImagePath(CurrentUserCode);
            var result = _userService.UpdateProfile(CurrentUserCode, contactNumber, null, newPhoto);

            if (result.Success)
            {
                if (newPhoto != null) _profilePhotos.Delete(oldPhoto);   // replaced photo: remove the old file
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                _profilePhotos.Delete(newPhoto);                         // save failed: drop the new upload
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Profile));
        }

        // ---------- Settings ----------
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