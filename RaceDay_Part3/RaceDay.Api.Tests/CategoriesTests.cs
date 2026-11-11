using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class CategoriesTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public CategoriesTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> RegisterAndLoginAsync(string email, string role)
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email,
            password = "Password123!",
            role
        });
        await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        return client;
    }

    private static async Task<int> CreateEventAsync(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            name = "Category Test Event",
            description = "For category tests.",
            eventDate = "2026-12-01T00:00:00Z",
            location = "Polokwane",
            distance = 10.0,
            eventType = "Run"
        });
        var ev = await response.Content.ReadFromJsonAsync<EventResponse>();
        return ev!.eventId;
    }

    [Fact]
    public async Task CreateCategory_AsOrganiser_ReturnsCreated()
    {
        var organiser = await RegisterAndLoginAsync("cat-org1@example.com", "Organiser");
        var eventId = await CreateEventAsync(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "5km", categoryDistance = 5.0 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsParticipant_ReturnsForbidden()
    {
        var organiser = await RegisterAndLoginAsync("cat-org2@example.com", "Organiser");
        var participant = await RegisterAndLoginAsync("cat-part2@example.com", "Participant");
        var eventId = await CreateEventAsync(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "5km", categoryDistance = 5.0 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_AsOwningOrganiser_ReturnsOk()
    {
        var organiser = await RegisterAndLoginAsync("cat-org3@example.com", "Organiser");
        var eventId = await CreateEventAsync(organiser);
        var created = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "5km", categoryDistance = 5.0 });
        var category = await created.Content.ReadFromJsonAsync<CategoryResponse>();

        var response = await organiser.PutAsJsonAsync(
            $"/api/events/{eventId}/categories/{category!.categoryId}",
            new { categoryName = "Under 18", minAge = 10, maxAge = 17, categoryDistance = 5.0 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_ByNonOwningOrganiser_ReturnsForbidden()
    {
        var owner = await RegisterAndLoginAsync("cat-org4@example.com", "Organiser");
        var other = await RegisterAndLoginAsync("cat-org5@example.com", "Organiser");
        var eventId = await CreateEventAsync(owner);
        var created = await owner.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "5km", categoryDistance = 5.0 });
        var category = await created.Content.ReadFromJsonAsync<CategoryResponse>();

        var response = await other.DeleteAsync($"/api/events/{eventId}/categories/{category!.categoryId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_AsOwningOrganiser_ReturnsNoContent()
    {
        var organiser = await RegisterAndLoginAsync("cat-org6@example.com", "Organiser");
        var eventId = await CreateEventAsync(organiser);
        var created = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "5km", categoryDistance = 5.0 });
        var category = await created.Content.ReadFromJsonAsync<CategoryResponse>();

        var response = await organiser.DeleteAsync($"/api/events/{eventId}/categories/{category!.categoryId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private record EventResponse(int eventId, string name);
    private record CategoryResponse(int categoryId, string categoryName);
}
