namespace RaceDay.Mvc.Services;

/// <summary>
/// Everything the MVC app knows about the logged-in user lives in ASP.NET Core Session —
/// including the raw API auth cookie, which ApiClient re-attaches to every API call it makes
/// on this user's behalf. This is what "session-based authentication" means for this app:
/// the browser holds an MVC session cookie, and the MVC server holds the API's cookie for it.
/// </summary>
public static class SessionExtensions
{
    private const string UserIdKey = "UserId";
    private const string FullNameKey = "FullName";
    private const string RoleKey = "Role";
    private const string AuthCookieKey = "ApiAuthCookie";

    public static void SignIn(this ISession session, int userId, string fullName, string role, string authCookie)
    {
        session.SetInt32(UserIdKey, userId);
        session.SetString(FullNameKey, fullName);
        session.SetString(RoleKey, role);
        session.SetString(AuthCookieKey, authCookie);
    }

    public static void SignOut(this ISession session)
    {
        session.Clear();
    }

    public static int? GetUserId(this ISession session) => session.GetInt32(UserIdKey);
    public static string? GetFullName(this ISession session) => session.GetString(FullNameKey);
    public static string? GetRole(this ISession session) => session.GetString(RoleKey);
    public static string? GetAuthCookie(this ISession session) => session.GetString(AuthCookieKey);
    public static bool IsLoggedIn(this ISession session) => session.GetUserId() is not null;
}
