using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventEnrolment> EventEnrolments => Set<EventEnrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Roles
        modelBuilder.Entity<Role>()
            .HasIndex(r => r.RoleName)
            .IsUnique();

        // Users
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Events
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser)
            .WithMany(u => u.OrganisedEvents)
            .HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Event>()
            .Property(e => e.Distance)
            .HasColumnType("decimal(6,2)");

        // Categories
        modelBuilder.Entity<Category>()
            .HasOne(c => c.Event)
            .WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Category>()
            .Property(c => c.CategoryDistance)
            .HasColumnType("decimal(6,2)");

        // EventEnrolments (key is named EnrolmentId, which EF does not detect by convention)
        modelBuilder.Entity<EventEnrolment>()
            .HasKey(en => en.EnrolmentId);

        modelBuilder.Entity<EventEnrolment>()
            .HasOne(en => en.Event)
            .WithMany(e => e.Enrolments)
            .HasForeignKey(en => en.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventEnrolment>()
            .HasOne(en => en.Category)
            .WithMany(c => c.Enrolments)
            .HasForeignKey(en => en.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventEnrolment>()
            .HasOne(en => en.Participant)
            .WithMany(u => u.Enrolments)
            .HasForeignKey(en => en.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        // One enrolment per participant per event
        modelBuilder.Entity<EventEnrolment>()
            .HasIndex(en => new { en.EventId, en.ParticipantId })
            .IsUnique();

        // Results — one-to-one with EventEnrolment
        modelBuilder.Entity<Result>()
            .HasOne(r => r.Enrolment)
            .WithOne(en => en.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Result>()
            .HasIndex(r => r.EnrolmentId)
            .IsUnique();

        // Seed the two fixed roles so registration can assign RoleId reliably
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Organiser" },
            new Role { RoleId = 2, RoleName = "Participant" }
        );
    }
}
