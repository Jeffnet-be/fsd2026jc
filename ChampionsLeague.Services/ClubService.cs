using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Services.DTOs;

namespace ChampionsLeague.Services;

/// <summary>
/// Service-laag contract voor club-operaties.
/// Retourneert <see cref="ClubDto"/> met alle velden die de Web-laag nodig heeft.
/// </summary>
public interface IClubService
{
    Task<IEnumerable<ClubDto>> GetAllWithStadiumsAsync();
    Task<IEnumerable<ClubDto>> GetAllAsync();
    Task<Club?> GetEntityWithStadiumAndSectorsAsync(int clubId);
}

/// <summary>
/// Implementatie van <see cref="IClubService"/>.
/// Mapt Club-entiteiten naar ClubDto — bevat PrimaryColor, Country en TotalCapacity
/// zodat de Web-laag ClubCardVM volledig kan vullen zonder zelf de entiteit te kennen.
/// </summary>
public class ClubService : IClubService
{
    private readonly IClubRepository _clubs;

    public ClubService(IClubRepository clubs)
    {
        _clubs = clubs;
    }

    public async Task<IEnumerable<ClubDto>> GetAllWithStadiumsAsync()
    {
        var clubs = await _clubs.GetAllWithStadiumsAsync();
        return clubs.Select(c => ToDto(c));
    }

    public async Task<IEnumerable<ClubDto>> GetAllAsync()
    {
        var clubs = await _clubs.GetAllAsync();
        return clubs.Select(c => new ClubDto
        {
            Id           = c.Id,
            Name         = c.Name,
            Country      = c.Country,
            BadgeUrl     = c.BadgeUrl     ?? string.Empty,
            PrimaryColor = c.PrimaryColor ?? "#000000"
        });
    }

    public Task<Club?> GetEntityWithStadiumAndSectorsAsync(int clubId)
        => _clubs.GetWithStadiumAndSectorsAsync(clubId);

    // ── Mapping ──────────────────────────────────────────────────────────

    private static ClubDto ToDto(Club c) => new()
    {
        Id            = c.Id,
        Name          = c.Name,
        Country       = c.Country,
        BadgeUrl      = c.BadgeUrl      ?? string.Empty,
        PrimaryColor  = c.PrimaryColor  ?? "#000000",
        StadiumName   = c.Stadium?.Name ?? string.Empty,
        StadiumCity   = c.Stadium?.City ?? string.Empty,
        TotalCapacity = c.Stadium?.Sectors.Sum(s => s.Capacity) ?? 0,
        Sectors       = c.Stadium?.Sectors.Select(s => new SectorDto
        {
            Id        = s.Id,
            Name      = s.Name,
            Capacity  = s.Capacity,
            BasePrice = s.BasePrice
        }).ToList() ?? new()
    };
}
