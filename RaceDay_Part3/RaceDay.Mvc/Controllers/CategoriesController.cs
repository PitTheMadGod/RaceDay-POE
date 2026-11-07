using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

/// <summary>Add age/distance categories to an event. Organiser only.</summary>
[RequireRole("Organiser")]
public class CategoriesController : Controller
{
    private readonly ApiClient _api;

    public CategoriesController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int eventId)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        if (ev is null) return NotFound();
        if (ev.OrganiserId != HttpContext.Session.GetUserId()) return View("~/Views/Shared/Forbidden.cshtml");

        return View(new CategoryFormViewModel { EventId = eventId, EventName = ev.Name });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _api.PostAsync<CategoryDto>($"api/events/{model.EventId}/categories", new
            {
                categoryName = model.CategoryName,
                minAge = model.MinAge,
                maxAge = model.MaxAge,
                categoryDistance = model.CategoryDistance
            });
        }
        catch (ApiException ex)
        {
            model.ErrorMessage = ex.Message;
            return View(model);
        }

        TempData["SuccessMessage"] = "Category added.";
        return RedirectToAction("Edit", "Events", new { id = model.EventId });
    }
}
