using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Handles image uploads (event banners, profile pictures) to Azure Blob Storage.
/// The MVC front end calls this first to get a URL, then sends that URL as part of
/// the event/profile payload — this keeps all Azure access inside the API, per the brief.
/// </summary>
[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IBlobStorageService _blobStorage;

    public FilesController(IBlobStorageService blobStorage)
    {
        _blobStorage = blobStorage;
    }

    /// <summary>Uploads an image file and returns its public blob URL.</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10_000_000)] // 10 MB
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file was uploaded." });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Only JPEG, PNG, GIF or WEBP images are allowed." });

        try
        {
            var url = await _blobStorage.UploadAsync(file);
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { message = "Could not reach Azure Blob Storage.", detail = ex.Message });
        }
    }
}
