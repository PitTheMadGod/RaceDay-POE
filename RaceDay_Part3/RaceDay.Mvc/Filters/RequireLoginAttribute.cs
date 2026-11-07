using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Filters;

/// <summary>
/// Blocks access to an action/controller unless the MVC session shows a logged-in user.
/// This is the MVC-side equivalent of the API's [Authorize] — unauthenticated users are
/// redirected to the login page instead of getting a 401, since this app serves HTML.
/// </summary>
public class RequireLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Session.IsLoggedIn())
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
        }
        base.OnActionExecuting(context);
    }
}
