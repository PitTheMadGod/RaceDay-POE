using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Filters;

/// <summary>
/// Restricts an action/controller to one role ("Organiser" or "Participant"). Implies
/// RequireLogin: an anonymous user is sent to the login page; a logged-in user with the
/// wrong role is sent to a 403 view rather than silently redirected, so they know why.
/// </summary>
public class RequireRoleAttribute : ActionFilterAttribute
{
    private readonly string _role;

    public RequireRoleAttribute(string role)
    {
        _role = role;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;

        if (!session.IsLoggedIn())
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (!string.Equals(session.GetRole(), _role, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new ViewResult { ViewName = "~/Views/Shared/Forbidden.cshtml" };
            return;
        }

        base.OnActionExecuting(context);
    }
}
