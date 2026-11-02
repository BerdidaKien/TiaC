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
        private readonly IWebHostEnvironment _env;

        public CustodianController(
            IEquipmentService equipmentService,
            IReservationService reservationService,
            IHistoryService historyService,
            IReportService reportService,
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

        // Builds the Equipment page: newest first, 5 per page
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
            model.CreatedBy = User.Identity?.Name ?? "Custodian";
            model.UpdatedBy = User.Identity?.Name ?? "Custodian";
            model.CreatedTime = DateTime.Now;
            model.UpdatedTime = DateTime.Now;

            ModelState.Remove(nameof(model.Category));
            ModelState.Remove(nameof(model.CreatedBy));
            ModelState.Remove(nameof(model.UpdatedBy));
            ModelState.Remove(nameof(model.BorrowingTransactions));
            ModelState.Remove(nameof(model.Reservations));
            ModelState.Remove(nameof(model.ImagePath));

            if (!_equipmentService.GetCategories().Any(c => c.Id == model.CategoryId))
            {
                ModelState.AddModelError(nameof(model.CategoryId), "Please select a valid category.");
            }

            var photoError = ValidatePhoto(photo);
            if (photoError != null)
            {
                ModelState.AddModelError("photo", photoError);
                TempData["ErrorMessage"] = photoError;
            }

            if (ModelState.IsValid)
            {
                model.ImagePath = SavePhoto(photo);   // null when no photo was chosen
                _equipmentService.AddEquipment(model);
                TempData["SuccessMessage"] = "Equipment asset registered successfully.";
                return RedirectToAction(nameof(Equipment));
            }

            return EquipmentPage(1);
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

            if (string.IsNullOrWhiteSpace(model.ItemCode) ||
                string.IsNullOrWhiteSpace(model.BrandModel) ||
                string.IsNullOrWhiteSpace(model.SerialNumber) ||
                string.IsNullOrWhiteSpace(model.StorageLocation))
            {
                TempData["ErrorMessage"] = "Item code, brand & model, serial number and storage location are required.";
                return RedirectToAction(nameof(Equipment), new { page });
            }

            var oldImagePath = _equipmentService.GetEquipmentById(model.Id)?.ImagePath;
            string newImagePath = null;

            model.ImagePath = null;
            if (photo != null && photo.Length > 0)
            {
                newImagePath = SavePhoto(photo);
                model.ImagePath = newImagePath;
            }

            model.UpdatedBy = User.Identity?.Name ?? "Custodian";
            var result = _equipmentService.UpdateEquipment(model);

            if (result.Success)
            {
                if (newImagePath != null) DeletePhotoFile(oldImagePath);   // replaced photo: remove the old file
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                DeletePhotoFile(newImagePath);                             // save failed: drop the new upload
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

        // Returns an error message, or null when the photo is fine (or not provided)
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

        // Saves to wwwroot/uploads/equipment with a random name; returns the web path
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

        // Removes a previously uploaded photo (only inside wwwroot/uploads/equipment)
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
            catch (IOException) { /* a leftover file is harmless */ }
        }

        private void PopulateCategoryList()
        {
            ViewBag.CategoryList = _equipmentService.GetCategories()
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.CategoryName
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

        // Print-ready page: the browser's print dialog saves it as PDF
        public IActionResult HistoryPrint(HistoryFilter filter)
        {
            return View(_historyService.GetPage(filter));
        }

        // ---------- Reports ----------
        public IActionResult Reports(DateTime? from, DateTime? to)
        {
            return View(_reportService.GetPage(from, to));
        }

        // JSON feed for the availability calendar (end is exclusive)
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
        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }
    }
}