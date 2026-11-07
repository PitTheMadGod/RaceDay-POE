using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

/// <summary>View and update the logged-in user's own profile, including a picture upload. Both roles.</summary>
[RequireLogin]
public class ProfileController : Controller
{
    private readonly ApiClient _api;

    public ProfileController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var profile = await _api.GetAsync<UserProfileDto>("api/users/me");
        if (profile is null) return NotFound();

        return View(new ProfileViewModel
        {
            FullName = profile.FullName,
            Email = profile.Email,
            Role = profile.Role,
            ExistingProfilePictureUrl = profile.ProfilePictureUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            string? pictureUrl = null;
            if (model.ProfilePicture is { Length: > 0 })
                pictureUrl = await _api.UploadFileAsync(model.ProfilePicture);

            await _api.PutAsync<object>("api/users/me", new
            {
                fullName = model.FullName,
                email = model.Email,
                profilePictureUrl = pictureUrl
            });

            model.SuccessMessage = "Profile updated.";
            if (pictureUrl is not null) model.ExistingProfilePictureUrl = pictureUrl;
        }
        catch (ApiException ex)
        {
            model.ErrorMessage = ex.Message;
        }

        return View(model);
    }
}
