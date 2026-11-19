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

        private readonly IInfoRequestService _infoRequestService;
        private readonly ProofStorage _proofStorage;

        public InfoRequestController(
            IInfoRequestService infoRequestService,
            ProofStorage proofStorage,
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IMapper mapper)
            : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            _infoRequestService = infoRequestService;
            _proofStorage = proofStorage;
        }

        private string CurrentUserCode => _session.GetString("UserId");

        [HttpGet]
        public IActionResult Create()
        {
            var model = _infoRequestService.GetForm(CurrentUserCode);
            if (model == null) return RedirectToAction("Login", "Account");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SubmitInfoRequest model, List<IFormFile> proofs)
        {
            var form = _infoRequestService.GetForm(CurrentUserCode);
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
                    if (!_proofStorage.TrySave(f, out var stored, out var fileError))
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
                result = _infoRequestService.Submit(CurrentUserCode, model, saved);
                if (!result.Success) error = result.Message;
            }

            if (error != null)
            {
                // Nothing was saved, so drop the uploaded files and show the form again
                foreach (var s in saved) _proofStorage.Delete(s.StoredName);

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
            var result = _infoRequestService.Cancel(id, CurrentUserCode);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Create));
        }
    }
}