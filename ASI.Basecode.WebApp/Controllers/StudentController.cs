// Location: ASI.Basecode.WebApp/Controllers/StudentController.cs
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
    [Authorize(Roles = "Student,user")]
    public class StudentController : ControllerBase<StudentController>
    {
        private readonly IEquipmentService _equipmentService;
        private readonly IReservationService _reservationService;

        public StudentController(
            IEquipmentService equipmentService,
            IReservationService reservationService,
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IMapper mapper) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            _equipmentService = equipmentService;
            _reservationService = reservationService;
        }

        private string CurrentUserCode => _session.GetString("UserId");

        /// <summary>
        /// Student Catalog / Dashboard
        /// </summary>
        public IActionResult Index(string search, int? categoryId, string status, int page = 1)
        {
            const int pageSize = 5;

            var catalog = _equipmentService.GetDashboardSummary(search, categoryId, status);
            var allItems = catalog.Items.ToList();

            var totalPages = Math.Max(1, (int)Math.Ceiling(allItems.Count / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var model = new StudentDashboardViewModel
            {
                Catalog = catalog,
                MyActiveBorrowings = _reservationService.CountActiveForUser(CurrentUserCode),
                MyPendingRequests = _reservationService.CountPendingForUser(CurrentUserCode),
                RecentReservations = _reservationService.GetForUser(CurrentUserCode).Take(5).ToList(),

                PagedItems = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalFiltered = allItems.Count
            };

            return View(model);
        }

        /// <summary>
        /// Search Equipment page: everything the custodian registered
        /// </summary>
        public IActionResult Equipment(string search, int? categoryId, string status, string sort, int page = 1)
        {
            // Load once, then filter in memory (safe even if the service returns IQueryable)
            IEnumerable<EquipmentItem> items = _equipmentService.GetAllEquipment()
                .ToList()
                .Where(i => i.Status == "available" || i.Status == "borrowed");   // hide maintenance/archived items

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                items = items.Where(i =>
                    (i.BrandModel ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (i.ItemCode ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (i.Category?.CategoryName ?? "").Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (categoryId.HasValue)
                items = items.Where(i => i.CategoryId == categoryId.Value);

            if (status == "available")
                items = items.Where(i => i.Status == "available");
            else if (status == "borrowed")
                items = items.Where(i => i.Status == "borrowed");

            items = sort == "category"
                ? items.OrderBy(i => i.Category?.CategoryName).ThenBy(i => i.BrandModel)
                : items.OrderBy(i => i.BrandModel);

            // ---- Pagination: 6 items per page ----
            const int pageSize = 6;
            var all = items.ToList();
            var totalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);
            var pageItems = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = all.Count;
            ViewBag.CategoryId = categoryId;

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            ViewBag.CategoryList = _equipmentService.GetCategories()
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.CategoryName,
                    Selected = categoryId == c.Id
                })
                .ToList();

            return View(pageItems);
        }

        /// <summary>
        /// Active and past equipment reservation history
        /// </summary>
        public IActionResult MyReservations()
        {
            return View(_reservationService.GetForUser(CurrentUserCode));
        }

        /// <summary>
        /// View specific equipment details before borrowing
        /// </summary>
        public IActionResult EquipmentDetails(int id)
        {
            return View();
        }
    }
}