using ChampionsLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChampionsLeague.Infrastructure.Data;

/// <summary>
/// Static seed class called from OnModelCreating.
/// HasData() embeds seed rows directly in the EF Core migration — no separate SQL scripts needed.
/// Six clubs, one stadium each, eight sectors per stadium, and a sample match calendar.
/// </summary>
public static class SeedData
{
    public static void Seed(ModelBuilder builder)
    {
        SeedClubs(builder);
        SeedStadiums(builder);
        SeedSectors(builder);
        SeedMatches(builder);
    }

    // ── Clubs ──────────────────────────────────────────────────────────
    private static void SeedClubs(ModelBuilder b)
    {
        b.Entity<Club>().HasData(
            new Club { Id = 1, Name = "Real Madrid",        Country = "Spain",   BadgeUrl = "/images/clubs/realmadrid.png",  PrimaryColor = "#FEBE10" },
            new Club { Id = 2, Name = "Manchester City",     Country = "England", BadgeUrl = "/images/clubs/mancity.png",    PrimaryColor = "#6CABDD" },
            new Club { Id = 3, Name = "FC Bayern München",   Country = "Germany", BadgeUrl = "/images/clubs/bayern.png",     PrimaryColor = "#DC052D" },
            new Club { Id = 4, Name = "Paris Saint-Germain", Country = "France",  BadgeUrl = "/images/clubs/psg.png",        PrimaryColor = "#004170" },
            new Club { Id = 5, Name = "Club Brugge",         Country = "Belgium", BadgeUrl = "/images/clubs/clubbrugge.png", PrimaryColor = "#002FA7" },
            new Club { Id = 6, Name = "FC Barcelona",        Country = "Spain",   BadgeUrl = "/images/clubs/barcelona.png",  PrimaryColor = "#A50044" }
        );
    }

    // ── Stadiums ───────────────────────────────────────────────────────
    private static void SeedStadiums(ModelBuilder b)
    {
        b.Entity<Stadium>().HasData(
            new Stadium { Id = 1, Name = "Santiago Bernabéu",   City = "Madrid",     ClubId = 1 },
            new Stadium { Id = 2, Name = "Etihad Stadium",       City = "Manchester", ClubId = 2 },
            new Stadium { Id = 3, Name = "Allianz Arena",        City = "Munich",     ClubId = 3 },
            new Stadium { Id = 4, Name = "Parc des Princes",     City = "Paris",      ClubId = 4 },
            new Stadium { Id = 5, Name = "Jan Breydel Stadion",  City = "Brugge",     ClubId = 5 },
            new Stadium { Id = 6, Name = "Camp Nou",             City = "Barcelona",  ClubId = 6 }
        );
    }

    // ── Sectors — 8 per stadium × 6 stadiums = 48 rows ────────────────
    private static void SeedSectors(ModelBuilder b)
    {
        var sectorNames = new[]
        {
            "Onderste ring – achter doel (thuisploeg)",
            "Onderste ring – achter doel (bezoekers)",
            "Onderste ring – zijlijn Oost",
            "Onderste ring – zijlijn West",
            "Bovenste ring – achter doel (thuisploeg)",
            "Bovenste ring – achter doel (bezoekers)",
            "Bovenste ring – zijlijn Oost",
            "Bovenste ring – zijlijn West"
        };

        // (stadiumId, capacity[8], basePrice[8])
        var configs = new (int StadiumId, int[] Caps, decimal[] Prices)[]
        {
            (1, new[]{3500,2500,4000,4000,2500,1800,3000,3000}, new decimal[]{95,70,120,120,55,40,75,75}),
            (2, new[]{3200,2200,3800,3800,2200,1500,2800,2800}, new decimal[]{90,65,115,115,50,38,70,70}),
            (3, new[]{3000,2000,3600,3600,2000,1400,2600,2600}, new decimal[]{85,60,110,110,48,35,68,68}),
            (4, new[]{2800,1800,3400,3400,1800,1200,2400,2400}, new decimal[]{80,55,105,105,45,32,65,65}),
            (5, new[]{2000,1200,2500,2500,1500, 900,2000,2000}, new decimal[]{55,35, 70, 70,32,22,45,45}),
            (6, new[]{3300,2300,3900,3900,2300,1600,2900,2900}, new decimal[]{92,68,118,118,52,40,72,72}),
        };

        var sectorTypes = Enum.GetValues<SectorType>();
        int id = 1;

        foreach (var cfg in configs)
        {
            for (int i = 0; i < 8; i++)
            {
                b.Entity<Sector>().HasData(new Sector
                {
                    Id        = id++,
                    StadiumId = cfg.StadiumId,
                    Name      = sectorNames[i],
                    Type      = sectorTypes[i],
                    Capacity  = cfg.Caps[i],
                    BasePrice = cfg.Prices[i]
                });
            }
        }
    }

    // ── Matches — sample group stage + knockout fixtures ───────────────
    private static void SeedMatches(ModelBuilder b)
    {
        var d = new DateTime(2025, 9, 17, 20, 45, 0, DateTimeKind.Utc);
        b.Entity<Match>().HasData(
            new Match { Id= 1, HomeClubId=1, AwayClubId=4, MatchDate=d,              Phase="Group Stage"   },
            new Match { Id= 2, HomeClubId=2, AwayClubId=3, MatchDate=d,              Phase="Group Stage"   },
            new Match { Id= 3, HomeClubId=5, AwayClubId=6, MatchDate=d,              Phase="Group Stage"   },
            new Match { Id= 4, HomeClubId=4, AwayClubId=2, MatchDate=d.AddDays(14),  Phase="Group Stage"   },
            new Match { Id= 5, HomeClubId=3, AwayClubId=1, MatchDate=d.AddDays(14),  Phase="Group Stage"   },
            new Match { Id= 6, HomeClubId=6, AwayClubId=5, MatchDate=d.AddDays(14),  Phase="Group Stage"   },
            new Match { Id= 7, HomeClubId=1, AwayClubId=2, MatchDate=d.AddDays(28),  Phase="Group Stage"   },
            new Match { Id= 8, HomeClubId=3, AwayClubId=6, MatchDate=d.AddDays(28),  Phase="Group Stage"   },
            new Match { Id= 9, HomeClubId=4, AwayClubId=5, MatchDate=d.AddDays(28),  Phase="Group Stage"   },
            new Match { Id=10, HomeClubId=2, AwayClubId=6, MatchDate=d.AddDays(56),  Phase="Quarter-Final" },
            new Match { Id=11, HomeClubId=1, AwayClubId=3, MatchDate=d.AddDays(56),  Phase="Quarter-Final" },
            new Match { Id=12, HomeClubId=5, AwayClubId=4, MatchDate=d.AddDays(70),  Phase="Semi-Final"    },
            new Match { Id=13, HomeClubId=6, AwayClubId=1, MatchDate=d.AddDays(84),  Phase="Final"         }
        );
    }
}
