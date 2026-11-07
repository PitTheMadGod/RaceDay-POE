using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services;

/// <summary>
/// Uploads files to Azure Blob Storage. Works against a real Azure Storage account
/// or against the Azurite emulator (see appsettings.json / the setup guide) — both
/// speak the same connection-string format, so no code changes are needed between them.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobStorageOptions _options;

    public BlobStorageService(IOptions<BlobStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        if (file.Length == 0)
            throw new ArgumentException("The uploaded file is empty.");

        var containerClient = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var extension = Path.GetExtension(file.FileName);
        var blobName = $"{Guid.NewGuid()}{extension}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blobClient.Uri.ToString();
    }
}
