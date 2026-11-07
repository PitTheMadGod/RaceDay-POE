using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

/// <summary>Event management - create, edit, delete and the Organiser's dashboard. Organiser only.</summary>
[RequireRole("Organiser")]
public class EventsController : Controller
{
    private readonly ApiClient _api;

    public EventsController(ApiClient api)
    {
        _api = api;
    }

    /// <summary>Summary dashboard: the Organiser's own events, enrolment counts, upcoming dates.</summary>
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var myId = HttpContext.Session.GetUserId();
        var events = (await _api.GetAsync<List<EventDto>>("api/events") ?? new())
            .Where(e => e.OrganiserId == myId)
            .OrderBy(e => e.EventDate)
            .ToList();

        var rows = new List<(EventDto Event, int EnrolmentCount)>();
        foreach (var ev in events)
        {
            var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{ev.EventId}/enrolments") ?? new();
            rows.Add((ev, enrolments.Count));
        }

        return View(rows);
    }

    /// <summary>Management list - same events as the dashboard, but with edit/delete actions.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var myId = HttpContext.Session.GetUserId();
        var events = (await _api.GetAsync<List<EventDto>>("api/events") ?? new())
            .Where(e => e.OrganiserId == myId)
            .OrderBy(e => e.EventDate)
            .ToList();

        return View(events);
    }

    [HttpGet]
    public IActionResult Create() => View(new EventFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            string? bannerUrl = null;
            if (model.BannerImage is { Length: > 0 })
                bannerUrl = await _api.UploadFileAsync(model.BannerImage);

            await _api.PostAsync<EventDto>("api/events", new
            {
                name = model.Name,
                description = model.Description,
                eventDate = model.EventDate,
                location = model.Location,
                distance = model.Distance,
                eventType = model.EventType,
                bannerImageUrl = bannerUrl
            });
        }
        catch (ApiException ex)
        {
            model.ErrorMessage = ex.Message;
            return View(model);
        }

        TempData["SuccessMessage"] = "Event created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{id}");
        if (ev is null) return NotFound();
        if (ev.OrganiserId != HttpContext.Session.GetUserId()) return View("~/Views/Shared/Forbidden.cshtml");

        return View(new EventFormViewModel
        {
            EventId = ev.EventId,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Distance = ev.Distance,
            EventType = ev.EventType,
            ExistingBannerImageUrl = ev.BannerImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            string? bannerUrl = null;
            if (model.BannerImage is { Length: > 0 })
                bannerUrl = await _api.UploadFileAsync(model.BannerImage);

            await _api.PutAsync<EventDto>($"api/events/{id}", new
            {
                name = model.Name,
                description = model.Description,
                eventDate = model.EventDate,
                location = model.Location,
                distance = model.Distance,
                eventType = model.EventType,
                bannerImageUrl = bannerUrl
            });
        }
        catch (ApiException ex)
        {
            model.ErrorMessage = ex.Message;
            return View(model);
        }

        TempData["SuccessMessage"] = "Event updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _api.DeleteAsync($"api/events/{id}");
            TempData["SuccessMessage"] = "Event deleted.";
        }
        catch (ApiException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
