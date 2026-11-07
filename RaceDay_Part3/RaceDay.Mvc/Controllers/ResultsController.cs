using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

/// <summary>Capturing results (Organiser) and viewing personal race history (Participant).</summary>
[RequireLogin]
public class ResultsController : Controller
{
    private readonly ApiClient _api;

    public ResultsController(ApiClient api)
    {
        _api = api;
    }

    /// <summary>Organiser: capture a finish time and position for one enrolment.</summary>
    [HttpGet]
    public async Task<IActionResult> Create(int enrolmentId, int eventId)
    {
        if (HttpContext.Session.GetRole() != "Organiser") return View("~/Views/Shared/Forbidden.cshtml");

        var ev = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments") ?? new();
        var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories") ?? new();
        var enrolment = enrolments.FirstOrDefault(e => e.EnrolmentId == enrolmentId);
        if (ev is null || enrolment is null) return NotFound();

        return View(new ResultFormViewModel
        {
            EnrolmentId = enrolmentId,
            EventName = ev.Name,
            ParticipantName = $"Participant #{enrolment.ParticipantId}",
            CategoryName = categories.FirstOrDefault(c => c.CategoryId == enrolment.CategoryId)?.CategoryName ?? "-"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ResultFormViewModel model, int eventId)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _api.PostAsync<ResultDto>($"api/enrolments/{model.EnrolmentId}/results", new
            {
                finishTime = model.FinishTime,
                finishPosition = model.FinishPosition
            });
        }
        catch (ApiException ex)
        {
            model.ErrorMessage = ex.Message;
            return View(model);
        }

        TempData["SuccessMessage"] = "Result captured.";
        return RedirectToAction("ForEvent", "Enrolments", new { eventId });
    }

    /// <summary>Participant: "My Results" page - full race history across completed events.</summary>
    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        if (HttpContext.Session.GetRole() != "Participant") return View("~/Views/Shared/Forbidden.cshtml");

        var results = await _api.GetAsync<List<ResultDto>>("api/users/me/results") ?? new();
        var enrolments = await _api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments") ?? new();
        var allEvents = await _api.GetAsync<List<EventDto>>("api/events") ?? new();

        var rows = new List<MyResultRowViewModel>();
        foreach (var r in results)
        {
            var enrolment = enrolments.FirstOrDefault(e => e.EnrolmentId == r.EnrolmentId);
            if (enrolment is null) continue;

            var ev = allEvents.FirstOrDefault(e => e.EventId == enrolment.EventId);
            if (ev is null) continue;

            var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{ev.EventId}/categories") ?? new();

            rows.Add(new MyResultRowViewModel
            {
                EventName = ev.Name,
                EventDate = ev.EventDate,
                CategoryName = categories.FirstOrDefault(c => c.CategoryId == enrolment.CategoryId)?.CategoryName ?? "-",
                FinishTime = r.FinishTime,
                FinishPosition = r.FinishPosition
            });
        }

        return View(rows.OrderByDescending(r => r.EventDate).ToList());
    }
}
