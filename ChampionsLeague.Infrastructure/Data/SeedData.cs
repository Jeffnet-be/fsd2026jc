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
            new Club { Id = 1, Name = "Real Madrid",        Country = "Spain",   BadgeUrl = "https://upload.wikimedia.org/wikipedia/en/5/56/Real_Madrid_CF.svg",                     PrimaryColor = "#FEBE10" },
            new Club { Id = 2, Name = "Manchester City",     Country = "England", BadgeUrl = "https://upload.wikimedia.org/wikipedia/en/e/eb/Manchester_City_FC_badge.svg",             PrimaryColor = "#6CABDD" },
            new Club { Id = 3, Name = "FC Bayern München",   Country = "Germany", BadgeUrl = "https://upload.wikimedia.org/wikipedia/commons/8/8d/FC_Bayern_M%C3%BCnchen_logo_%282024%29.svg", PrimaryColor = "#DC052D" },
            new Club { Id = 4, Name = "Paris Saint-Germain", Country = "France",  BadgeUrl = "https://upload.wikimedia.org/wikipedia/en/a/a7/Paris_Saint-Germain_F.C..svg",             PrimaryColor = "#004170" },
            new Club { Id = 5, Name = "Club Brugge",         Country = "Belgium", BadgeUrl = "https://upload.wikimedia.org/wikipedia/en/d/d0/Club_Brugge_KV_logo.svg",                  PrimaryColor = "#002FA7" },
            new Club { Id = 6, Name = "FC Barcelona",        Country = "Spain",   BadgeUrl = "https://upload.wikimedia.org/wikipedia/en/4/47/FC_Barcelona_%28crest%29.svg",              PrimaryColor = "#A50044" }
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
    // Dates are set in the near future relative to April 2026:
    // - Matchday 1 (Apr 22): sale OPEN  — within 1 month window
    // - Matchday 2 (Apr 29): sale OPEN  — within 1 month window
    // - Matchday 3 (May 13): sale CLOSED — more than 1 month away
    // - Knockouts: sale CLOSED
    // This correctly demonstrates the "1 month before" business rule.
    private static void SeedMatches(ModelBuilder b)
    {
        var d1 = new DateTime(2026, 4, 22, 20, 45, 0, DateTimeKind.Utc); // Matchday 1 — OPEN
        var d2 = new DateTime(2026, 4, 29, 20, 45, 0, DateTimeKind.Utc); // Matchday 2 — OPEN
        var d3 = new DateTime(2026, 5, 13, 20, 45, 0, DateTimeKind.Utc); // Matchday 3 — CLOSED
        var qf = new DateTime(2026, 6, 3,  20, 45, 0, DateTimeKind.Utc); // Quarter-Finals
        var sf = new DateTime(2026, 7, 8,  20, 45, 0, DateTimeKind.Utc); // Semi-Final
        var fi = new DateTime(2026, 8, 12, 20, 45, 0, DateTimeKind.Utc); // Final
        b.Entity<Match>().HasData(
            new Match { Id= 1, HomeClubId=1, AwayClubId=4, MatchDate=d1, Phase="Group Stage"   },
            new Match { Id= 2, HomeClubId=2, AwayClubId=3, MatchDate=d1, Phase="Group Stage"   },
            new Match { Id= 3, HomeClubId=5, AwayClubId=6, MatchDate=d1, Phase="Group Stage"   },
            new Match { Id= 4, HomeClubId=4, AwayClubId=2, MatchDate=d2, Phase="Group Stage"   },
            new Match { Id= 5, HomeClubId=3, AwayClubId=1, MatchDate=d2, Phase="Group Stage"   },
            new Match { Id= 6, HomeClubId=6, AwayClubId=5, MatchDate=d2, Phase="Group Stage"   },
            new Match { Id= 7, HomeClubId=1, AwayClubId=2, MatchDate=d3, Phase="Group Stage"   },
            new Match { Id= 8, HomeClubId=3, AwayClubId=6, MatchDate=d3, Phase="Group Stage"   },
            new Match { Id= 9, HomeClubId=4, AwayClubId=5, MatchDate=d3, Phase="Group Stage"   },
            new Match { Id=10, HomeClubId=2, AwayClubId=6, MatchDate=qf, Phase="Quarter-Final" },
            new Match { Id=11, HomeClubId=1, AwayClubId=3, MatchDate=qf, Phase="Quarter-Final" },
            new Match { Id=12, HomeClubId=5, AwayClubId=4, MatchDate=sf, Phase="Semi-Final"    },
            new Match { Id=13, HomeClubId=6, AwayClubId=1, MatchDate=fi, Phase="Final"         }
        );
    }
}
