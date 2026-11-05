using System.Collections.Generic;
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
    // Students and custodians ask the admin to correct their name, ID or email
    [Authorize(Roles = "Student,User,user,Custodian")]
    public class InfoRequestController : ControllerBase<InfoRequestController>
    {
        private const int MaxFiles = 3;

        private readonly IInfoRequestService _service;
        private readonly ProofStorage _storage;

        public InfoRequestController(
            IInfoRequestService service,
            ProofStorage storage,
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IMapper mapper)
            : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            _service = service;
            _storage = storage;
        }

        private string CurrentUserCode => _session.GetString("UserId");

        [HttpGet]
        public IActionResult Create()
        {
            var model = _service.GetForm(CurrentUserCode);
            if (model == null) return RedirectToAction("Login", "Account");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SubmitInfoRequest model, List<IFormFile> proofs)
        {
            var form = _service.GetForm(CurrentUserCode);
            if (form == null) return RedirectToAction("Login", "Account");

            var files = (proofs ?? new List<IFormFile>()).Where(f => f != null && f.Length > 0).ToList();
            var saved = new List<StoredProof>();
            string error = null;

            if (files.Count > MaxFiles)
            {
                error = $"You can attach up to {MaxFiles} photos.";
            }
            else
            {
                foreach (var f in files)
                {
                    if (!_storage.TrySave(f, out var stored, out var fileError))
                    {
                        error = fileError;
                        break;
                    }
                    saved.Add(stored);
                }
            }

            ServiceResult result = null;
            if (error == null)
            {
                result = _service.Submit(CurrentUserCode, model, saved);
                if (!result.Success) error = result.Message;
            }

            if (error != null)
            {
                // Nothing was saved, so drop the uploaded files and show the form again
                foreach (var s in saved) _storage.Delete(s.StoredName);

                form.Posted = model;
                ViewBag.Error = error;
                return View(form);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Create));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            var result = _service.Cancel(id, CurrentUserCode);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Create));
        }
    }
}