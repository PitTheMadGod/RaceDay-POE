using RaceDay.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Session - this is how the MVC app remembers who is logged in and holds the
// API auth cookie it re-sends on every call (see Services/SessionExtensions.cs).
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDayMvcSession";
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// Typed HttpClient pointed at the Part 2 API. Every request the MVC app makes for
// data goes through here - no controller talks to a database directly.
builder.Services.AddHttpClient("RaceDayApi", client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});
builder.Services.AddScoped<ApiClient>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
