using System;
using System.Collections.Generic;
using System.Linq;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
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

        public CustodianController(
            IEquipmentService equipmentService,
            IReservationService reservationService,
            IHistoryService historyService,
            IReportService reportService,
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
        }

        // ---------- Dashboard ----------
        public IActionResult Index(string search, int? categoryId, string status)
        {
            var model = _equipmentService.GetDashboardSummary(search, categoryId, status);
            return View(model);
        }

        // ---------- Equipment ----------
        public IActionResult Equipment()
        {
            var equipmentList = _equipmentService.GetAllEquipment();
            PopulateCategoryList();
            return View(equipmentList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddEquipment(EquipmentItem model)
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

            if (!_equipmentService.GetCategories().Any(c => c.Id == model.CategoryId))
            {
                ModelState.AddModelError(nameof(model.CategoryId), "Please select a valid category.");
            }

            if (ModelState.IsValid)
            {
                _equipmentService.AddEquipment(model);
                TempData["SuccessMessage"] = "Equipment asset registered successfully.";
                return RedirectToAction(nameof(Equipment));
            }

            PopulateCategoryList();
            return View("Equipment", _equipmentService.GetAllEquipment());
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