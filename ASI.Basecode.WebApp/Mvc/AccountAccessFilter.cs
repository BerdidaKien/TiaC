using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ASI.Basecode.Resources.Constants;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ASI.Basecode.WebApp.Mvc
{
    /// <summary>
    /// Runs on every page for a signed-in user:
    /// - an account that was disabled or made inactive is signed out right away, instead of
    ///   staying logged in until the login cookie expires;
    /// - an account created by an admin must replace its temporary password before anything else.
    /// </summary>
    public class AccountAccessFilter : IAsyncActionFilter
    {
        private readonly IUserService _userService;

        public AccountAccessFilter(IUserService userService)
        {
            _userService = userService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;
            var principal = http.User;

            // Login, logout and password reset pages stay reachable
            if (principal.Identity == null || !principal.Identity.IsAuthenticated ||
                context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            {
                await next();
                return;
            }

            var userId = principal.FindFirst("UserId")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var access = string.IsNullOrEmpty(userId) ? AccountAccess.Blocked : _userService.GetAccountAccess(userId);

            if (access == AccountAccess.Blocked)
            {
                await http.SignOutAsync(Const.AuthenticationScheme);
                http.Session.Clear();

                if (context.Controller is Controller controller)
                {
                    controller.TempData["ErrorMessage"] = "Your account is no longer active. Please contact an administrator.";
                }

                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // The session (20 min idle) can expire while the login cookie (120 min) is still valid.
            // Pages read the user from the session, so refill it from the login cookie's claims.
            if (string.IsNullOrEmpty(http.Session.GetString("UserId")))
            {
                http.Session.SetString("UserId", userId);
                http.Session.SetString("UserName", principal.FindFirst("UserName")?.Value ?? "");
                http.Session.SetString("UserRole", principal.FindFirst(ClaimTypes.Role)?.Value ?? "Student");
                http.Session.SetString("UserPhoto", _userService.GetProfileImagePath(userId) ?? "");
            }

            if (access == AccountAccess.MustSetUp && !principal.IsInRole("Admin"))
            {
                var controllerName = context.RouteData.Values["controller"] as string;
                var actionName = context.RouteData.Values["action"] as string;
                var onSetupPage = string.Equals(controllerName, "Account", System.StringComparison.OrdinalIgnoreCase) &&
                                  string.Equals(actionName, "FirstLogin", System.StringComparison.OrdinalIgnoreCase);

                if (!onSetupPage)
                {
                    context.Result = new RedirectToActionResult("FirstLogin", "Account", null);
                    return;
                }
            }

            await next();
        }
    }
}
