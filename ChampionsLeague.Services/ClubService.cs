using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Services.DTOs;

namespace ChampionsLeague.Services;

/// <summary>
/// Service-laag contract voor club-operaties.
/// Gebruikt <see cref="ClubDto"/> — geen Web.ViewModels.
/// </summary>
public interface IClubService
{
    /// <summary>Alle clubs met stadion- en sectordata, als DTOs.</summary>
    Task<IEnumerable<ClubDto>> GetAllWithStadiumsAsync();

    /// <summary>Alle clubs (naam + id), als DTOs.</summary>
    Task<IEnumerable<ClubDto>> GetAllAsync();

    /// <summary>
    /// Één club met stadion en sectoren op basis van clubId.
    /// Geeft de ruwe entiteit terug voor gebruik in TicketService/SeasonTicketService.
    /// </summary>
    Task<Club?> GetEntityWithStadiumAndSectorsAsync(int clubId);
}

/// <summary>
/// Implementatie van <see cref="IClubService"/>.
/// Mapt Club-entiteiten naar ClubDto — geen Web-afhankelijkheden.
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
        return clubs.Select(ToDto);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ClubDto>> GetAllAsync()
    {
        var clubs = await _clubs.GetAllAsync();
        return clubs.Select(c => new ClubDto { Id = c.Id, Name = c.Name, BadgeUrl = c.BadgeUrl ?? string.Empty });
    }

    /// <inheritdoc/>
    public Task<Club?> GetEntityWithStadiumAndSectorsAsync(int clubId)
        => _clubs.GetWithStadiumAndSectorsAsync(clubId);

    // ── Mapping ──────────────────────────────────────────────────────────

    private static ClubDto ToDto(Club c) => new()
    {
        Id          = c.Id,
        Name        = c.Name,
        BadgeUrl    = c.BadgeUrl    ?? string.Empty,
        StadiumName = c.Stadium?.Name ?? string.Empty,
        StadiumCity = c.Stadium?.City ?? string.Empty,
        Sectors     = c.Stadium?.Sectors.Select(s => new SectorDto
        {
            Id        = s.Id,
            Name      = s.Name,
            Capacity  = s.Capacity,
            BasePrice = s.BasePrice
        }).ToList() ?? new()
    };
}
