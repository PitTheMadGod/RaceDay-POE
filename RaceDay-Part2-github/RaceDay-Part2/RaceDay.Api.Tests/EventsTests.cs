using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class EventsTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public EventsTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<HttpClient> RegisterAndLoginAsync(
        RaceDayWebApplicationFactory factory, string email, string role)
    {
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email,
            password = "Password123!",
            role
        });

        await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });

        return client;
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_ReturnsCreated()
    {
        var client = await RegisterAndLoginAsync(_factory, "organiser1@example.com", "Organiser");

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            name = "Test Run",
            description = "A test event.",
            eventDate = "2026-12-01T00:00:00Z",
            location = "Polokwane",
            distance = 10.0,
            eventType = "Run"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        var client = await RegisterAndLoginAsync(_factory, "participant1@example.com", "Participant");

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            name = "Should Fail",
            description = "Participants cannot create events.",
            eventDate = "2026-12-01T00:00:00Z",
            location = "Polokwane",
            distance = 10.0,
            eventType = "Run"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidEventType_ReturnsBadRequest()
    {
        var client = await RegisterAndLoginAsync(_factory, "organiser2@example.com", "Organiser");

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            name = "Bad Type Event",
            description = "Invalid event type.",
            eventDate = "2026-12-01T00:00:00Z",
            location = "Polokwane",
            distance = 10.0,
            eventType = "Swim"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllEvents_WhenLoggedIn_ReturnsOk()
    {
        var client = await RegisterAndLoginAsync(_factory, "viewer1@example.com", "Participant");
        var response = await client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllEvents_WithoutLoggingIn_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ByNonOwningOrganiser_ReturnsForbidden()
    {
        var owner = await RegisterAndLoginAsync(_factory, "owner1@example.com", "Organiser");
        var createResponse = await owner.PostAsJsonAsync("/api/events", new
        {
            name = "Owned Event",
            description = "Owned by owner1.",
            eventDate = "2026-12-01T00:00:00Z",
            location = "Polokwane",
            distance = 5.0,
            eventType = "Walk"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        var otherOrganiser = await RegisterAndLoginAsync(_factory, "other-owner1@example.com", "Organiser");
        var deleteResponse = await otherOrganiser.DeleteAsync($"/api/events/{created!.eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    private record EventResponse(int eventId, string name);
}
