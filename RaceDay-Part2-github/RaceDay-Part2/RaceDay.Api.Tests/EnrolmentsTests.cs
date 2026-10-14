using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class EnrolmentsTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public EnrolmentsTests(RaceDayWebApplicationFactory factory)
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
        await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        return client;
    }

    private async Task<(int eventId, int categoryId)> CreateEventWithCategoryAsync(HttpClient organiserClient)
    {
        var eventResponse = await organiserClient.PostAsJsonAsync("/api/events", new
        {
            name = "Enrolment Test Event",
            description = "For enrolment tests.",
            eventDate = "2026-12-01T00:00:00Z",
            location = "Polokwane",
            distance = 10.0,
            eventType = "Run"
        });
        var ev = await eventResponse.Content.ReadFromJsonAsync<EventResponse>();

        var categoryResponse = await organiserClient.PostAsJsonAsync(
            $"/api/events/{ev!.eventId}/categories", new
            {
                categoryName = "10km",
                categoryDistance = 10.0
            });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        return (ev.eventId, category!.categoryId);
    }

    [Fact]
    public async Task Enrol_AsParticipant_ReturnsCreated()
    {
        var organiser = await RegisterAndLoginAsync(_factory, "organiser-enrol1@example.com", "Organiser");
        var (eventId, categoryId) = await CreateEventWithCategoryAsync(organiser);

        var participant = await RegisterAndLoginAsync(_factory, "participant-enrol1@example.com", "Participant");
        var response = await participant.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_Twice_ReturnsConflict()
    {
        var organiser = await RegisterAndLoginAsync(_factory, "organiser-enrol2@example.com", "Organiser");
        var (eventId, categoryId) = await CreateEventWithCategoryAsync(organiser);

        var participant = await RegisterAndLoginAsync(_factory, "participant-enrol2@example.com", "Participant");
        await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        var secondResponse = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_ReturnsForbidden()
    {
        var organiser = await RegisterAndLoginAsync(_factory, "organiser-enrol3@example.com", "Organiser");
        var (eventId, categoryId) = await CreateEventWithCategoryAsync(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private record EventResponse(int eventId, string name);
    private record CategoryResponse(int categoryId, string categoryName);
}
