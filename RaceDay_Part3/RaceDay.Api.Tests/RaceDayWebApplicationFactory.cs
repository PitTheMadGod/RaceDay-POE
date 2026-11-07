using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

/// <summary>
/// Swaps the real SQL Server DbContext for an isolated EF Core InMemory
/// database per test run, so tests need no SQL Server instance at all.
/// </summary>
public class RaceDayWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(DatabaseName));

            // Ensure the seeded Roles exist for this in-memory database.
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
