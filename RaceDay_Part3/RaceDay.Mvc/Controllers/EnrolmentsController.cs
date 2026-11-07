using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

/// <summary>
/// Enrolling in an event (Participant) and viewing who's enrolled (Organiser).
/// Login is required for everything here, but the two roles see different actions.
/// </summary>
[RequireLogin]
public class EnrolmentsController : Controller
{
    private readonly ApiClient _api;

    public EnrolmentsController(ApiClient api)
    {
        _api = api;
    }

    /// <summary>Organiser: lists every Participant enrolled in one of their events.</summary>
    [HttpGet]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        if (HttpContext.Session.GetRole() != "Organiser") return View("~/Views/Shared/Forbidden.cshtml");

        var ev = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        if (ev is null) return NotFound();

        try
        {
            var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments") ?? new();
            var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories") ?? new();
            var results = await _api.GetAsync<List<ResultDto>>($"api/events/{eventId}/results") ?? new();

            var rows = enrolments.Select(en => new EnrolmentRowViewModel
            {
                EnrolmentId = en.EnrolmentId,
                ParticipantName = $"Participant #{en.ParticipantId}",
                CategoryName = categories.FirstOrDefault(c => c.CategoryId == en.CategoryId)?.CategoryName ?? "-",
                EnrolmentDate = en.EnrolmentDate,
                HasResult = results.Any(r => r.EnrolmentId == en.EnrolmentId)
            }).ToList();

            ViewBag.Event = ev;
            return View(rows);
        }
        catch (ApiException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Index", "Events");
        }
    }

    /// <summary>Participant: shows the chosen event's categories and confirms enrolment.</summary>
    [HttpGet]
    public async Task<IActionResult> Enrol(int eventId)
    {
        if (HttpContext.Session.GetRole() != "Participant") return View("~/Views/Shared/Forbidden.cshtml");

        var ev = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        if (ev is null) return NotFound();

        var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories") ?? new();
        ViewBag.Event = ev;
        ViewBag.Categories = categories;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        try
        {
            await _api.PostAsync<EnrolmentDto>($"api/events/{eventId}/enrolments", new { categoryId });
            TempData["SuccessMessage"] = "You're enrolled! It shows up under \"My Enrolments\" immediately.";
        }
        catch (ApiException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction("Details", "Home", new { id = eventId });
    }

    /// <summary>Participant: their own enrolment history with status.</summary>
    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        if (HttpContext.Session.GetRole() != "Participant") return View("~/Views/Shared/Forbidden.cshtml");

        var enrolments = await _api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments") ?? new();
        var allEvents = await _api.GetAsync<List<EventDto>>("api/events") ?? new();

        var rows = new List<MyEnrolmentRowViewModel>();
        foreach (var en in enrolments)
        {
            var ev = allEvents.FirstOrDefault(e => e.EventId == en.EventId);
            if (ev is null) continue;

            var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{en.EventId}/categories") ?? new();

            rows.Add(new MyEnrolmentRowViewModel
            {
                EventId = ev.EventId,
                EventName = ev.Name,
                EventDate = ev.EventDate,
                CategoryName = categories.FirstOrDefault(c => c.CategoryId == en.CategoryId)?.CategoryName ?? "-",
                Status = ev.EventDate < DateTime.Today ? "Completed" : "Confirmed"
            });
        }

        return View(rows.OrderByDescending(r => r.EventDate).ToList());
    }
}
