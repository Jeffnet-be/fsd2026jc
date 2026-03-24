using FullStackDevelopment_Ticketverkoop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FullStackDevelopment_Ticketverkoop.Data;

/// <summary>
/// Seeds the database with the 6 required clubs, their stadiums,
/// 8 section types each, and a set of sample matches.
/// Called once at application startup if the database is empty.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Clubs.AnyAsync()) return; // Already seeded

        var clubs = new List<Club>
        {
            new() { Name = "Real Madrid",       Country = "Spain",   LogoUrl = "/images/clubs/realmadrid.png" },
            new() { Name = "Manchester City",   Country = "England", LogoUrl = "/images/clubs/mancity.png" },
            new() { Name = "FC Bayern München", Country = "Germany", LogoUrl = "/images/clubs/bayern.png" },
            new() { Name = "Paris Saint-Germain",Country= "France",  LogoUrl = "/images/clubs/psg.png" },
            new() { Name = "Club Brugge",       Country = "Belgium", LogoUrl = "/images/clubs/brugge.png" },
            new() { Name = "FC Barcelona",      Country = "Spain",   LogoUrl = "/images/clubs/barcelona.png" },
        };
        context.Clubs.AddRange(clubs);
        await context.SaveChangesAsync();

        // Section type names as per project spec
        var sectionNames = new[]
        {
            "Lower ring – behind goal (home)",
            "Lower ring – behind goal (away)",
            "Lower ring – east sideline",
            "Lower ring – west sideline",
            "Upper ring – behind goal (home)",
            "Upper ring – behind goal (away)",
            "Upper ring – east sideline",
            "Upper ring – west sideline",
        };

        decimal[] prices = { 85m, 75m, 110m, 110m, 55m, 45m, 70m, 70m };

        foreach (var club in clubs)
        {
            var stadium = new Stadium
            {
                Name = $"{club.Name} Stadium",
                City = club.Country,
                ClubId = club.Id,
            };
            context.Stadiums.Add(stadium);
            await context.SaveChangesAsync();

            for (int i = 0; i < sectionNames.Length; i++)
            {
                context.SectionTypes.Add(new SectionType
                {
                    Name = sectionNames[i],
                    Capacity = 1500,
                    Price = prices[i],
                    StadiumId = stadium.Id,
                });
            }
        }
        await context.SaveChangesAsync();

        // Seed sample matches (round-robin style)
        var savedClubs = await context.Clubs.ToListAsync();
        var matchDate = new DateTime(2025, 10, 1, 20, 45, 0);

        for (int i = 0; i < savedClubs.Count; i++)
        {
            var away = savedClubs[(i + 1) % savedClubs.Count];
            context.Matches.Add(new Domain.Entities.Match
            {
                HomeClubId = savedClubs[i].Id,
                AwayClubId = away.Id,
                MatchDate = matchDate.AddDays(i * 7),
                Phase = "Group Stage",
            });
        }
        await context.SaveChangesAsync();
    }
}