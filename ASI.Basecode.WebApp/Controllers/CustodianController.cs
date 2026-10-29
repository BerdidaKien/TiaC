using System;
using System.Collections.Generic;
using System.Linq;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
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

        public CustodianController(
            IEquipmentService equipmentService,
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IMapper mapper)
            : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            _equipmentService = equipmentService;
        }

        public IActionResult Index()
        {
            return View();
        }

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

            // Clear navigation properties from model state validation
            ModelState.Remove(nameof(model.Category));
            ModelState.Remove(nameof(model.CreatedBy));
            ModelState.Remove(nameof(model.UpdatedBy));
            ModelState.Remove(nameof(model.BorrowingTransactions));
            ModelState.Remove(nameof(model.Reservations));

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
            var categories = _equipmentService.GetCategories().ToList();

            if (categories != null && categories.Any())
            {
                // Value MUST be c.Id.ToString() so it posts integer primary keys (1, 2, 3...) to CategoryId
                ViewBag.CategoryList = categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.CategoryName
                    })
                    .ToList();
            }
            else
            {
                // Hardcoded Fallback matching SQL Server primary keys (1 - 6)
                ViewBag.CategoryList = new List<SelectListItem>
        {
            new SelectListItem { Value = "1", Text = "Audio-Visual & Multimedia" },
            new SelectListItem { Value = "2", Text = "IT & Computing Devices" },
            new SelectListItem { Value = "3", Text = "Event & Stage Supplies" },
            new SelectListItem { Value = "4", Text = "Science & Lab Apparatus" },
            new SelectListItem { Value = "5", Text = "Sports & Athletics" },
            new SelectListItem { Value = "6", Text = "Facilities & General Tools" }
        };
            }
        }

        public IActionResult Reservations()
        {
            return View();
        }

        public IActionResult History()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }

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