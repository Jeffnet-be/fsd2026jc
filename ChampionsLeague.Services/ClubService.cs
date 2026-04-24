using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Services.DTOs;

namespace ChampionsLeague.Services;

/// <summary>
/// Service-laag contract voor club-operaties.
/// </summary>
public interface IClubService
{
    /// <summary>Alle clubs als DTOs — voor controllers die mappen naar ViewModels.</summary>
    Task<IEnumerable<ClubDto>> GetAllWithStadiumsAsync();

    /// <summary>Alle clubs (naam + id) als DTOs.</summary>
    Task<IEnumerable<ClubDto>> GetAllAsync();

    /// <summary>
    /// Alle clubs als ruwe domein-entiteiten.
    /// Gebruikt door views die rechtstreeks Club.PrimaryColor,
    /// Club.Stadium.Sectors etc. aanspreken (bv. SeasonTicket/Index.cshtml).
    /// </summary>
    Task<IEnumerable<Club>> GetAllEntitiesWithStadiumsAsync();

    /// <summary>Één club met stadion en sectoren als ruwe entiteit.</summary>
    Task<Club?> GetEntityWithStadiumAndSectorsAsync(int clubId);
}

/// <summary>
/// Implementatie van <see cref="IClubService"/>.
/// </summary>
public class ClubService : IClubService
{
    private readonly IClubRepository _clubs;

    public ClubService(IClubRepository clubs)
    {
        _clubs = clubs;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ClubDto>> GetAllWithStadiumsAsync()
    {
        var clubs = await _clubs.GetAllWithStadiumsAsync();
        return clubs.Select(c => ToDto(c));
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public Task<IEnumerable<Club>> GetAllEntitiesWithStadiumsAsync()
        => _clubs.GetAllWithStadiumsAsync();

    /// <inheritdoc/>
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
