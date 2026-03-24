using ChampionsLeague.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChampionsLeague.Infrastructure.Data;

/// <summary>
/// Central EF Core DbContext. Inherits from IdentityDbContext so ASP.NET Core Identity
/// tables (AspNetUsers, AspNetRoles, etc.) are automatically included in the same database.
/// This is the single "session with the database" described in chapter 10 of the course notes.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    /// <param name="options">
    /// Injected by the DI container — contains the connection string and provider.
    /// This is Constructor Injection (DI type 1 from curriculum section 6.16).
    /// </param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── DbSets — one per aggregate root entity ──────────────────────────
    public DbSet<Club>         Clubs         => Set<Club>();
    public DbSet<Stadium>      Stadiums      => Set<Stadium>();
    public DbSet<Sector>       Sectors       => Set<Sector>();
    public DbSet<Match>        Matches       => Set<Match>();
    public DbSet<Ticket>       Tickets       => Set<Ticket>();
    public DbSet<Order>        Orders        => Set<Order>();
    public DbSet<OrderLine>    OrderLines    => Set<OrderLine>();
    public DbSet<SeasonTicket> SeasonTickets => Set<SeasonTicket>();

    /// <summary>
    /// Fluent API configuration: table constraints, relationships, and seed data.
    /// Fluent API is preferred over DataAnnotations for complex mappings
    /// (course section 10.1.1 — Key Concepts in EF Core).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // MUST call base — sets up Identity tables

        // ── Club ──────────────────────────────────────────────────────
        builder.Entity<Club>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired().HasMaxLength(100);
            e.HasOne(c => c.Stadium)
             .WithOne(s => s.Club)
             .HasForeignKey<Stadium>(s => s.ClubId);
        });

        // ── Stadium ───────────────────────────────────────────────────
        builder.Entity<Stadium>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).IsRequired().HasMaxLength(150);
            e.HasMany(s => s.Sectors)
             .WithOne(sec => sec.Stadium)
             .HasForeignKey(sec => sec.StadiumId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Sector ────────────────────────────────────────────────────
        builder.Entity<Sector>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.BasePrice).HasColumnType("decimal(10,2)");
        });

        // ── Match — two FKs to Club require explicit names to avoid ambiguity ──
        builder.Entity<Match>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasOne(m => m.HomeClub)
             .WithMany(c => c.HomeMatches)
             .HasForeignKey(m => m.HomeClubId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.AwayClub)
             .WithMany(c => c.AwayMatches)
             .HasForeignKey(m => m.AwayClubId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Ticket ────────────────────────────────────────────────────
        builder.Entity<Ticket>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.VoucherId).IsUnique();
            e.Property(t => t.PricePaid).HasColumnType("decimal(10,2)");
            e.HasOne(t => t.Match)
             .WithMany(m => m.Tickets)
             .HasForeignKey(t => t.MatchId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Sector)
             .WithMany(s => s.Tickets)
             .HasForeignKey(t => t.SectorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.OrderLine)
             .WithMany(ol => ol.Tickets)
             .HasForeignKey(t => t.OrderLineId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Order ─────────────────────────────────────────────────────
        builder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.TotalAmount).HasColumnType("decimal(10,2)");
            e.HasOne(o => o.User)
             .WithMany(u => u.Orders)
             .HasForeignKey(o => o.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── OrderLine ─────────────────────────────────────────────────
        builder.Entity<OrderLine>(e =>
        {
            e.HasKey(ol => ol.Id);
            e.Property(ol => ol.UnitPrice).HasColumnType("decimal(10,2)");
            e.Ignore(ol => ol.LineTotal); // computed property — not stored in DB
            e.HasOne(ol => ol.Order)
             .WithMany(o => o.OrderLines)
             .HasForeignKey(ol => ol.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ol => ol.Match)
             .WithMany(m => m.OrderLines)
             .HasForeignKey(ol => ol.MatchId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ol => ol.Sector)
             .WithMany()
             .HasForeignKey(ol => ol.SectorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SeasonTicket ──────────────────────────────────────────────
        builder.Entity<SeasonTicket>(e =>
        {
            e.HasKey(st => st.Id);
            e.Property(st => st.TotalPrice).HasColumnType("decimal(10,2)");
            e.HasOne(st => st.User)
             .WithMany()
             .HasForeignKey(st => st.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(st => st.Sector)
             .WithMany(s => s.SeasonTickets)
             .HasForeignKey(st => st.SectorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Seed data ─────────────────────────────────────────────────
        SeedData.Seed(builder);
    }
}
