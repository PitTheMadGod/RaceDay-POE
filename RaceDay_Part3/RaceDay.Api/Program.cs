using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// EF Core - SQL Server, code-first
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Azure Blob Storage (event banners, profile pictures) - see appsettings.json "AzureBlobStorage"
builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection("AzureBlobStorage"));
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

// The MVC front end runs on a different origin/port, so it needs CORS to call this API
// with its session cookie attached.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcClient", policy =>
    {
        var mvcOrigin = builder.Configuration["MvcClientOrigin"] ?? "http://localhost:5100";
        policy.WithOrigins(mvcOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Session-based (cookie) authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RaceDayAuth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        // Return plain status codes instead of redirecting to a login page - this is an API, not a web app.
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API for the RaceDay event management system. " +
                      "Log in via POST /api/auth/login first - the session cookie is then sent automatically. " +
                      "Organisers manage events, categories and results; Participants enrol and view their own enrolments and results."
    });

    // Documents the cookie-based session login so Swagger shows how the API is secured.
    options.AddSecurityDefinition("RaceDayAuth", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Cookie,
        Name = "RaceDayAuth",
        Description = "Session cookie issued by POST /api/auth/login."
    });
});

var app = builder.Build();

// Swagger is enabled in all environments so markers can verify the live API against the Part 1 plan.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
});

app.UseCors("AllowMvcClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed so the test project's WebApplicationFactory<Program> can bootstrap the app in-memory.
public partial class Program { }
