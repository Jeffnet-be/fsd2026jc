using FullStackDevelopment_Ticketverkoop.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FullStackDevelopment_Ticketverkoop.Data;

/// <summary>
/// The central EF Core database context.
/// Inherits from IdentityDbContext so ASP.NET Core Identity tables
/// (Users, Roles, etc.) are created automatically alongside our domain tables.
/// </summary>
public class AppDbContext : IdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Club> Clubs { get; set; }
    public DbSet<Stadium> Stadiums { get; set; }
    public DbSet<SectionType> SectionTypes { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
    public DbSet<SeasonTicket> SeasonTickets { get; set; }

    /// <summary>
    /// Fluent API configuration — used to set up relationships,
    /// constraints, and default values that cannot be expressed
    /// purely with data annotations.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Required for Identity tables

        // A match has a home club and an away club — two separate FK relationships
        builder.Entity<Match>()
            .HasOne(m => m.HomeClub)
            .WithMany(c => c.HomeMatches)
            .HasForeignKey(m => m.HomeClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Match>()
            .HasOne(m => m.AwayClub)
            .WithMany(c => c.AwayMatches)
            .HasForeignKey(m => m.AwayClubId)
            .OnDelete(DeleteBehavior.Restrict);

        // Prevent double-booking: each (Match, SectionType, SeatRow, SeatNumber) is unique
        builder.Entity<Ticket>()
            .HasIndex(t => new { t.MatchId, t.SectionTypeId, t.SeatRow, t.SeatNumber })
            .IsUnique();

        // Decimal precision for money fields
        builder.Entity<Ticket>().Property(t => t.Price).HasPrecision(10, 2);
        builder.Entity<Order>().Property(o => o.TotalPrice).HasPrecision(10, 2);
        builder.Entity<OrderLine>().Property(ol => ol.UnitPrice).HasPrecision(10, 2);
        builder.Entity<SectionType>().Property(s => s.Price).HasPrecision(10, 2);
        builder.Entity<SeasonTicket>().Property(s => s.Price).HasPrecision(10, 2);
    }
}