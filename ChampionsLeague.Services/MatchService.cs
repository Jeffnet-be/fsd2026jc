using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Services.DTOs;

namespace ChampionsLeague.Services;

/// <summary>
/// Implementatie van <see cref="IMatchService"/>.
/// Mapt Domain-entiteiten naar <see cref="MatchDto"/> / <see cref="MatchDetailDto"/>.
/// Geen enkele import van ChampionsLeague.Web.
/// </summary>
public class MatchService : IMatchService
{
    private readonly IMatchRepository _matches;
    private readonly IClubRepository  _clubs;

    public MatchService(IMatchRepository matches, IClubRepository clubs)
    {
        _matches = matches;
        _clubs   = clubs;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<MatchDto>> GetAllAsync()
    {
        var entities = await _matches.GetAllWithClubsAsync();
        return entities.Select(x => ToDto(x));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<MatchDto>> GetByClubAsync(int clubId)
    {
        var entities = await _matches.GetByClubAsync(clubId);
        return entities.Select(x => ToDto(x));
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Match>> GetAllWithClubsAsync()
        => _matches.GetAllWithClubsAsync();

    /// <summary>
    /// Bouwt MatchDetailDto op: combineert match-info en sector-beschikbaarheid.
    /// Deze logica hoort hier in de service, niet in de controller.
    /// Geeft null terug als de wedstrijd niet bestaat.
    /// </summary>
    public async Task<MatchDetailDto?> GetDetailAsync(int matchId)
    {
        var allMatches = await _matches.GetAllWithClubsAsync();
        var match      = allMatches.FirstOrDefault(m => m.Id == matchId);
        if (match is null) return null;

        var club    = await _clubs.GetWithStadiumAndSectorsAsync(match.HomeClubId);
        var sectors = club?.Stadium?.Sectors?.ToList() ?? new List<Sector>();

        // Beschikbaarheid sequentieel berekenen — EF Core DbContext is niet thread-safe
        var sectorDtos = new List<SectorAvailabilityDto>();
        foreach (var sec in sectors)
        {
            int sold = 0;
            try { sold = await _matches.GetSoldCountAsync(matchId, sec.Id); }
            catch { /* bij fout: 0 verkocht, pagina crasht niet */ }

            sectorDtos.Add(new SectorAvailabilityDto
            {
                SectorId   = sec.Id,
                SectorName = sec.Name,
                Capacity   = sec.Capacity,
                Available  = Math.Max(0, sec.Capacity - sold),
                Price      = sec.BasePrice
            });
        }

        return new MatchDetailDto
        {
            Match   = ToDto(match, club),
            Sectors = sectorDtos
        };
    }

    // ── Mapping helpers ──────────────────────────────────────────────────

    private static MatchDto ToDto(Match m, ChampionsLeague.Domain.Entities.Club? club = null)
        => new()
        {
            Id            = m.Id,
            HomeClubId    = m.HomeClubId,
            HomeClubName  = m.HomeClub?.Name     ?? string.Empty,
            HomeClubBadge = m.HomeClub?.BadgeUrl ?? string.Empty,
            AwayClubId    = m.AwayClubId,
            AwayClubName  = m.AwayClub?.Name     ?? string.Empty,
            AwayClubBadge = m.AwayClub?.BadgeUrl ?? string.Empty,
            MatchDate     = m.MatchDate,
            Phase         = m.Phase,
            StadiumName   = club?.Stadium?.Name  ?? m.HomeClub?.Stadium?.Name ?? string.Empty,
            StadiumCity   = club?.Stadium?.City  ?? string.Empty,
            IsSaleOpen    = m.IsSaleOpen
        };
}
