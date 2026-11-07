using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

/// <summary>
/// The Participant-facing landing page (upcoming events, with filtering) and the public
/// event detail page. A logged-in Organiser is sent straight to their own dashboard instead,
/// since this list isn't meant for them.
/// </summary>
public class HomeController : Controller
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? eventType, string? search)
    {
        if (HttpContext.Session.GetRole() == "Organiser")
            return RedirectToAction("Dashboard", "Events");

        var events = await _api.GetAsync<List<EventDto>>("api/events") ?? new();

        var upcoming = events.Where(e => e.EventDate >= DateTime.Today);

        if (!string.IsNullOrWhiteSpace(eventType))
            upcoming = upcoming.Where(e => string.Equals(e.EventType, eventType, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search))
            upcoming = upcoming.Where(e =>
                e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));

        ViewBag.EventType = eventType;
        ViewBag.Search = search;

        return View(upcoming.OrderBy(e => e.EventDate).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var ev = await _api.GetAsync<EventDto>($"api/events/{id}");
            var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories") ?? new();
            ViewBag.Categories = categories;
            return View(ev);
        }
        catch (ApiException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    public IActionResult Error() => View();
}
