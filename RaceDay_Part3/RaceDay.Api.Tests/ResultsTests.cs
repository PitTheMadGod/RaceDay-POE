using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class ResultsTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public ResultsTests(RaceDayWebApplicationFactory factory)
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

    /// <summary>Creates an event + category as the organiser and enrols the participant. Returns the enrolment id.</summary>
    private static async Task<int> CreateEnrolmentAsync(HttpClient organiser, HttpClient participant)
    {
        var eventResponse = await organiser.PostAsJsonAsync("/api/events", new
        {
            name = "Results Test Event",
            description = "For results tests.",
            eventDate = "2026-12-01T00:00:00Z",
            location = "Polokwane",
            distance = 10.0,
            eventType = "Run"
        });
        var ev = await eventResponse.Content.ReadFromJsonAsync<EventResponse>();

        var categoryResponse = await organiser.PostAsJsonAsync($"/api/events/{ev!.eventId}/categories",
            new { categoryName = "10km", categoryDistance = 10.0 });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var enrolResponse = await participant.PostAsJsonAsync($"/api/events/{ev.eventId}/enrolments",
            new { categoryId = category!.categoryId });
        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponse>();
        return enrolment!.enrolmentId;
    }

    [Fact]
    public async Task CaptureResult_AsOrganiser_ReturnsCreated()
    {
        var organiser = await RegisterAndLoginAsync("res-org1@example.com", "Organiser");
        var participant = await RegisterAndLoginAsync("res-part1@example.com", "Participant");
        var enrolmentId = await CreateEnrolmentAsync(organiser, participant);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:52:14", finishPosition = 47 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_AsParticipant_ReturnsForbidden()
    {
        var organiser = await RegisterAndLoginAsync("res-org2@example.com", "Organiser");
        var participant = await RegisterAndLoginAsync("res-part2@example.com", "Participant");
        var enrolmentId = await CreateEnrolmentAsync(organiser, participant);

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:52:14", finishPosition = 47 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithBadTimeFormat_ReturnsBadRequest()
    {
        var organiser = await RegisterAndLoginAsync("res-org3@example.com", "Organiser");
        var participant = await RegisterAndLoginAsync("res-part3@example.com", "Participant");
        var enrolmentId = await CreateEnrolmentAsync(organiser, participant);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "fast", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_ReturnsConflict()
    {
        var organiser = await RegisterAndLoginAsync("res-org4@example.com", "Organiser");
        var participant = await RegisterAndLoginAsync("res-part4@example.com", "Participant");
        var enrolmentId = await CreateEnrolmentAsync(organiser, participant);
        var body = new { finishTime = "00:52:14", finishPosition = 47 };

        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", body);
        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task GetMyResults_AsParticipant_ReturnsOk()
    {
        var organiser = await RegisterAndLoginAsync("res-org5@example.com", "Organiser");
        var participant = await RegisterAndLoginAsync("res-part5@example.com", "Participant");
        var enrolmentId = await CreateEnrolmentAsync(organiser, participant);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:52:14", finishPosition = 47 });

        var response = await participant.GetAsync("/api/users/me/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAndDeleteResult_AsOwningOrganiser_Succeed()
    {
        var organiser = await RegisterAndLoginAsync("res-org6@example.com", "Organiser");
        var participant = await RegisterAndLoginAsync("res-part6@example.com", "Participant");
        var enrolmentId = await CreateEnrolmentAsync(organiser, participant);
        var created = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:52:14", finishPosition = 47 });
        var result = await created.Content.ReadFromJsonAsync<ResultResponse>();

        var update = await organiser.PutAsJsonAsync($"/api/results/{result!.resultId}",
            new { finishTime = "00:50:00", finishPosition = 40 });
        var delete = await organiser.DeleteAsync($"/api/results/{result.resultId}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    private record EventResponse(int eventId, string name);
    private record CategoryResponse(int categoryId, string categoryName);
    private record EnrolmentResponse(int enrolmentId);
    private record ResultResponse(int resultId);
}
