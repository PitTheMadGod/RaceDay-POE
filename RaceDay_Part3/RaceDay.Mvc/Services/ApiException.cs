using System.Net;

namespace RaceDay.Mvc.Services;

/// <summary>Thrown by ApiClient when the API returns a non-success status code.</summary>
public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public ApiException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
