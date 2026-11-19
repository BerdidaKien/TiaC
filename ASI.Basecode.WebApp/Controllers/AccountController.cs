using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.Services.Services;
using ASI.Basecode.WebApp.Authentication;
using ASI.Basecode.WebApp.Models;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.WebApp.Controllers
{
    public class AccountController : ControllerBase<AccountController>
    {
        private readonly SessionManager _sessionManager;
        private readonly SignInManager _signInManager;
        private readonly TokenValidationParametersFactory _tokenValidationParametersFactory;
        private readonly TokenProviderOptionsFactory _tokenProviderOptionsFactory;
        private readonly IUserService _userService;

        private readonly IPasswordResetService _passwordResetService;

        public AccountController(
            SignInManager signInManager,
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IMapper mapper,
            IUserService userService,
            IPasswordResetService passwordResetService,
            TokenValidationParametersFactory tokenValidationParametersFactory,
            TokenProviderOptionsFactory tokenProviderOptionsFactory) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            _sessionManager = new SessionManager(_session);
            _signInManager = signInManager;
            _tokenProviderOptionsFactory = tokenProviderOptionsFactory;
            _tokenValidationParametersFactory = tokenValidationParametersFactory;
            _userService = userService;
            _passwordResetService = passwordResetService;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            TempData["returnUrl"] = System.Net.WebUtility.UrlDecode(HttpContext.Request.Query["ReturnUrl"]);

            _sessionManager.Clear();
            _session.SetString("SessionId", System.Guid.NewGuid().ToString());

            return this.View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl)
        {
            _session.SetString("HasSession", "Exist");

            User user = null;

            var loginResult = _userService.AuthenticateUser(model.UserId, model.Password, ref user);

            if (loginResult == LoginResult.Success)
            {
                await _signInManager.SignInAsync(user);

                var fullName = $"{user.FirstName} {user.LastName}".Trim();

                _session.SetString("UserName", fullName);
                _session.SetString("UserId", user.UserId);
                _session.SetString("UserRole", user.Role ?? "Student");
                _session.SetString("UserPhoto", user.ProfileImagePath ?? "");   // shown in the sidebar

                var role = user.Role?.ToLowerInvariant() ?? "student";

                switch (role)
                {
                    case "admin":
                        return RedirectToAction("Index", "Admin");

                    case "custodian":
                        return RedirectToAction("Index", "Custodian");

                    case "student":
                    case "user":
                    default:
                        return RedirectToAction("Index", "Student");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Incorrect UserId or Password";
                return View();
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                _passwordResetService.RequestReset(model.UserId, token =>
                    Url.Action("ResetPassword", "Account", new { token }, Request.Scheme));
            }
            catch (Exception ex)
            {
                // Log it, but show the same message so IDs can't be guessed
                _logger.LogError(ex, "Failed to send password reset email.");
            }

            TempData["SuccessMessage"] = "If that User ID exists, a reset link has been sent to its email.";
            return RedirectToAction("ForgotPassword");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login");
            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (_passwordResetService.ResetPassword(model.Token, model.NewPassword))
            {
                TempData["SuccessMessage"] = "Password updated. You can log in now.";
                return RedirectToAction("Login");
            }

            TempData["ErrorMessage"] = "This reset link is invalid or has expired. Please request a new one.";
            return RedirectToAction("ForgotPassword");
        }
        [AllowAnonymous]
        public async Task<IActionResult> SignOutUser()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account");
        }

        // ---------- First login (accounts created by an admin) ----------
        private string CurrentUserCode =>
            User.FindFirst("UserId")?.Value ?? _session.GetString("UserId");

        private IActionResult RedirectToOwnHome()
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Index", "Admin");
            if (User.IsInRole("Custodian")) return RedirectToAction("Index", "Custodian");
            return RedirectToAction("Index", "Student");
        }

        private FirstLoginViewModel BuildFirstLoginModel(string userCode)
        {
            var profile = _userService.GetProfile(userCode);
            if (profile == null) return null;

            return new FirstLoginViewModel
            {
                FullName = profile.FullName,
                UserId = profile.UserId,
                Email = profile.Email,
                Role = profile.Role
            };
        }

        [HttpGet]
        public IActionResult FirstLogin()
        {
            var userCode = CurrentUserCode;
            if (string.IsNullOrEmpty(userCode)) return RedirectToAction("Login", "Account");
            if (!_userService.MustCompleteSetup(userCode)) return RedirectToOwnHome();

            var model = BuildFirstLoginModel(userCode);
            if (model == null) return RedirectToAction("Login", "Account");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FirstLogin(FirstLoginViewModel posted)
        {
            var userCode = CurrentUserCode;
            if (string.IsNullOrEmpty(userCode)) return RedirectToAction("Login", "Account");
            if (!_userService.MustCompleteSetup(userCode)) return RedirectToOwnHome();

            posted ??= new FirstLoginViewModel();

            ServiceResult result = posted.NewPassword != posted.ConfirmPassword
                ? ServiceResult.Fail("New password and confirmation do not match.")
                : _userService.CompleteFirstLogin(userCode, posted.NewPassword);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToOwnHome();
            }

            // Passwords are never sent back; show what to fix
            var model = BuildFirstLoginModel(userCode);
            if (model == null) return RedirectToAction("Login", "Account");
            TempData["ErrorMessage"] = result.Message;
            return View(model);
        }

        // Each role has its own profile page; this one is the student's
        private IActionResult RedirectToOwnProfile()
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Account", "Admin");
            if (User.IsInRole("Custodian")) return RedirectToAction("Profile", "Custodian");
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult Profile()
        {
            if (User.IsInRole("Admin") || User.IsInRole("Custodian"))
            {
                return RedirectToOwnProfile();
            }

            var userCode = _session.GetString("UserId");

            if (string.IsNullOrEmpty(userCode))
            {
                return RedirectToAction("Login", "Account");
            }

            var profile = _userService.GetProfile(userCode);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "Student information could not be found.";
                return RedirectToAction("Login", "Account");
            }

            return View("~/Views/Student/Profile.cshtml", profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(string ContactNumber, string Email, IFormFile photo)
        {
            var userCode = _session.GetString("UserId");

            if (string.IsNullOrEmpty(userCode))
            {
                return RedirectToAction("Login", "Account");
            }

            var webRoot = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath;

            var photoError = ProfilePhotoHelper.Validate(photo);
            if (photoError != null)
            {
                TempData["ErrorMessage"] = photoError;
                return RedirectToAction(nameof(Profile));
            }

            var oldPath = _userService.GetProfileImagePath(userCode);
            var newPath = ProfilePhotoHelper.Save(photo, webRoot);        // null when no photo was chosen

            var result = _userService.UpdateProfile(userCode, ContactNumber, Email, newPath);

            if (result.Success)
            {
                if (newPath != null)
                {
                    ProfilePhotoHelper.Delete(oldPath, webRoot);          // remove the replaced photo
                    _session.SetString("UserPhoto", newPath);        // sidebar picks it up
                }

                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                ProfilePhotoHelper.Delete(newPath, webRoot);              // save failed: drop the new upload
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation do not match.";
                return RedirectToOwnProfile();
            }

            var passwordError = UserFieldRules.CheckPassword(NewPassword);
            if (passwordError != null)
            {
                TempData["ErrorMessage"] = passwordError;
                return RedirectToOwnProfile();
            }

            if (NewPassword == CurrentPassword)
            {
                TempData["ErrorMessage"] = "New password must be different from the current one.";
                return RedirectToOwnProfile();
            }

            var userCode = _session.GetString("UserId");

            if (string.IsNullOrEmpty(userCode))
            {
                return RedirectToAction("Login", "Account");
            }

            if (_userService.ChangePassword(userCode, CurrentPassword, NewPassword))
            {
                TempData["SuccessMessage"] = "Your password has been updated.";
            }
            else
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
            }

            return RedirectToOwnProfile();
        }
    }
}