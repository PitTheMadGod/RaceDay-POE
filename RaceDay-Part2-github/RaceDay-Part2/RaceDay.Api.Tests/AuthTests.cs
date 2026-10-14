using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class AuthTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public AuthTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Thabo Nkosi",
            email = "thabo.register@example.com",
            password = "Password123!",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            fullName = "Duplicate User",
            email = "duplicate@example.com",
            password = "Password123!",
            role = "Participant"
        };

        await client.PostAsJsonAsync("/api/auth/register", payload);
        var secondResponse = await client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Bad Role",
            email = "badrole@example.com",
            password = "Password123!",
            role = "Administrator"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Login Success",
            email = "login.success@example.com",
            password = "Password123!",
            role = "Organiser"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "login.success@example.com",
            password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Login Fail",
            email = "login.fail@example.com",
            password = "Password123!",
            role = "Organiser"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "login.fail@example.com",
            password = "WrongPassword!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithoutLoggingIn_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
