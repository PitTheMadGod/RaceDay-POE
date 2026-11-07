namespace RaceDay.Api.Services;

/// <summary>Bound from the "AzureBlobStorage" section of appsettings.json.</summary>
public class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "raceday-uploads";
}
