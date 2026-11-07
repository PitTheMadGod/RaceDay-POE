using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Services;

/// <summary>
/// The MVC app's only way of talking to the Part 2 API — no controller or view in this
/// project touches a database directly. Every call re-attaches the current user's API
/// auth cookie (stored in Session by SessionExtensions) so the API's role checks apply.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _contextAccessor;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor contextAccessor)
    {
        _http = httpClientFactory.CreateClient("RaceDayApi");
        _contextAccessor = contextAccessor;
    }

    private ISession Session => _contextAccessor.HttpContext!.Session;

    // ---- Auth (the only two calls that don't already have a cookie to attach) ----

    public async Task<(int UserId, string FullName, string Role)> LoginAsync(string email, string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(new { email, password }, options: JsonOptions)
        };

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new ApiException(response.StatusCode, await ReadErrorMessageAsync(response));

        var authCookie = ExtractAuthCookie(response)
            ?? throw new ApiException(HttpStatusCode.InternalServerError, "The API did not return an auth cookie.");

        var body = await response.Content.ReadFromJsonAsync<LoginResult>(JsonOptions)
            ?? throw new ApiException(HttpStatusCode.InternalServerError, "Unexpected response from the API.");

        Session.SignIn(body.UserId, body.FullName, body.Role, authCookie);
        return (body.UserId, body.FullName, body.Role);
    }

    public async Task RegisterAsync(string fullName, string email, string password, string role)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/register")
        {
            Content = JsonContent.Create(new { fullName, email, password, role }, options: JsonOptions)
        };

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new ApiException(response.StatusCode, await ReadErrorMessageAsync(response));
    }

    public async Task LogoutAsync()
    {
        try
        {
            await SendAsync(HttpMethod.Post, "api/auth/logout");
        }
        catch (ApiException)
        {
            // Session cookie may already be stale - that's fine, we're clearing it locally anyway.
        }
        finally
        {
            Session.SignOut();
        }
    }

    // ---- Generic JSON calls, all with the session's auth cookie attached ----

    public Task<T?> GetAsync<T>(string path) => SendAsync<T>(HttpMethod.Get, path);

    public Task<T?> PostAsync<T>(string path, object body) => SendAsync<T>(HttpMethod.Post, path, body);

    public Task PostAsync(string path, object body) => SendAsync(HttpMethod.Post, path, body);

    public Task<T?> PutAsync<T>(string path, object body) => SendAsync<T>(HttpMethod.Put, path, body);

    public Task DeleteAsync(string path) => SendAsync(HttpMethod.Delete, path);

    /// <summary>Uploads a single file as multipart/form-data and returns the blob URL.</summary>
    public async Task<string> UploadFileAsync(IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(streamContent, "file", file.FileName);

        var request = new HttpRequestMessage(HttpMethod.Post, "api/files/upload") { Content = content };
        AttachAuthCookie(request);

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new ApiException(response.StatusCode, await ReadErrorMessageAsync(response));

        var result = await response.Content.ReadFromJsonAsync<UploadResultDto>(JsonOptions)
            ?? throw new ApiException(HttpStatusCode.InternalServerError, "Unexpected upload response from the API.");
        return result.Url;
    }

    // ---- internals ----

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);
        AttachAuthCookie(request);

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new ApiException(response.StatusCode, await ReadErrorMessageAsync(response));

        if (response.StatusCode == HttpStatusCode.NoContent)
            return default;

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private async Task SendAsync(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);
        AttachAuthCookie(request);

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new ApiException(response.StatusCode, await ReadErrorMessageAsync(response));
    }

    private void AttachAuthCookie(HttpRequestMessage request)
    {
        var cookie = Session.GetAuthCookie();
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.Add("Cookie", cookie);
    }

    private static string? ExtractAuthCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;

        // We only need the name=value pair, not the Set-Cookie attributes (Path, HttpOnly, etc.)
        // - those are for a browser to interpret, not for us re-sending it as a Cookie header.
        foreach (var raw in cookies)
        {
            if (raw.StartsWith("RaceDayAuth=", StringComparison.OrdinalIgnoreCase))
                return raw.Split(';')[0];
        }
        return null;
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>(JsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.Message))
                return error.Message;
        }
        catch (JsonException)
        {
            // Response body wasn't JSON - fall through to the generic message below.
        }
        return $"The API returned {(int)response.StatusCode} {response.StatusCode}.";
    }

    private class LoginResult
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
