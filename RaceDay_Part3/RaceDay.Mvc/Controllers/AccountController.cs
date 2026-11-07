using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

/// <summary>Registration, login and logout - the only pages a visitor can reach while logged out.</summary>
public class AccountController : Controller
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.IsLoggedIn())
            return RedirectToAction("Index", "Home");

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        try
        {
            await _api.LoginAsync(model.Email, model.Password);
        }
        catch (ApiException ex)
        {
            model.ErrorMessage = ex.Message;
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsLoggedIn())
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _api.RegisterAsync(model.FullName, model.Email, model.Password, model.Role);
        }
        catch (ApiException ex)
        {
            model.ErrorMessage = ex.Message;
            return View(model);
        }

        TempData["SuccessMessage"] = "Account created! You can log in now.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.LogoutAsync();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult Forbidden() => View("~/Views/Shared/Forbidden.cshtml");
}
