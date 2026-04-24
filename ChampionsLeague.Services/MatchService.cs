using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Services.ViewModels;

namespace ChampionsLeague.Services;

/// <summary>
/// Implementatie van <see cref="IMatchService"/>.
/// Combineert data uit meerdere repositories en past businesslogica toe.
/// Leeft in de Services-laag zodat de Web-laag geen repository-interfaces hoeft te kennen.
/// </summary>
public class MatchService : IMatchService
{
    private readonly IMatchRepository _matches;
    private readonly IClubRepository  _clubs;

    /// <summary>
    /// Repositories worden via constructor-injectie aangeleverd.
    /// MatchService kent de interfaces, niet de concrete implementaties —
    /// dit maakt unit-testing eenvoudig via mock-objecten.
    /// </summary>
    public MatchService(IMatchRepository matches, IClubRepository clubs)
    {
        _matches = matches;
        _clubs   = clubs;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Match>> GetAllWithClubsAsync()
        => _matches.GetAllWithClubsAsync();

    /// <inheritdoc/>
    public Task<IEnumerable<Match>> GetByClubAsync(int clubId)
        => _matches.GetByClubAsync(clubId);

    /// <inheritdoc/>
    public Task<int> GetSoldCountAsync(int matchId, int sectorId)
        => _matches.GetSoldCountAsync(matchId, sectorId);

    /// <summary>
    /// Bouwt het volledige MatchDetailVM op voor de detailpagina.
    /// Logica die data combineert uit meerdere repositories hoort in de service,
    /// niet in de controller — controllers zijn verantwoordelijk voor HTTP, niet voor data.
    /// </summary>
    public async Task<MatchDetailVM?> GetDetailAsync(int matchId)
    {
        var allMatches = await _matches.GetAllWithClubsAsync();
        var match      = allMatches.FirstOrDefault(m => m.Id == matchId);
        if (match is null) return null;

        var club    = await _clubs.GetWithStadiumAndSectorsAsync(match.HomeClubId);
        var sectors = club?.Stadium?.Sectors?.ToList() ?? new List<Sector>();

        // Beschikbaarheid per sector berekenen — sequentieel om EF Core thread-safety te garanderen
        var sectorVms = new List<SectorAvailabilityVM>();
        foreach (var sec in sectors)
        {
            int sold = 0;
            try { sold = await _matches.GetSoldCountAsync(matchId, sec.Id); }
            catch { sold = 0; }

            sectorVms.Add(new SectorAvailabilityVM
            {
                SectorId   = sec.Id,
                SectorName = sec.Name,
                Capacity   = sec.Capacity,
                Available  = Math.Max(0, sec.Capacity - sold),
                Price      = sec.BasePrice
            });
        }

        return new MatchDetailVM
        {
            Id            = match.Id,
            HomeClubName  = match.HomeClub?.Name  ?? string.Empty,
            AwayClubName  = match.AwayClub?.Name  ?? string.Empty,
            HomeClubBadge = match.HomeClub?.BadgeUrl ?? string.Empty,
            AwayClubBadge = match.AwayClub?.BadgeUrl ?? string.Empty,
            MatchDate     = match.MatchDate,
            Phase         = match.Phase,
            StadiumName   = club?.Stadium?.Name ?? string.Empty,
            StadiumCity   = club?.Stadium?.City ?? string.Empty,
            IsSaleOpen    = match.IsSaleOpen,
            Sectors       = sectorVms
        };
    }
}
