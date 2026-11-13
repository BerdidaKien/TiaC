using System;
using System.Linq;
using System.Security.Claims;
using ASI.Basecode.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ASI.Basecode.WebApp.Mvc
{
    /// <summary>
    /// Accounts an admin created must replace their temporary password before using anything else.
    /// Every page redirects to Account/FirstLogin until that is done.
    /// </summary>
    public class FirstLoginFilter : IActionFilter
    {
        private readonly IUserService _userService;

        public FirstLoginFilter(IUserService userService)
        {
            _userService = userService;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var principal = context.HttpContext.User;
            if (principal.Identity == null || !principal.Identity.IsAuthenticated) return;
            if (principal.IsInRole("Admin")) return;

            // Login, logout and password reset pages stay reachable
            if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()) return;

            var controller = context.RouteData.Values["controller"] as string;
            var action = context.RouteData.Values["action"] as string;
            if (string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(action, "FirstLogin", StringComparison.OrdinalIgnoreCase)) return;

            var userId = principal.FindFirst("UserId")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.MustCompleteSetup(userId)) return;

            context.Result = new RedirectToActionResult("FirstLogin", "Account", null);
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
