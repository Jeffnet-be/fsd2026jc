using AutoMapper;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Displays the match calendar with optional club filter.
/// The Index view uses jQuery DataTables for client-side ordering, searching, and paging.
/// </summary>
public class MatchesController : Controller
{
    private readonly IMatchRepository _matches;
    private readonly IClubRepository  _clubs;
    private readonly IMapper          _mapper;

    public MatchesController(
        IMatchRepository matches,
        IClubRepository  clubs,
        IMapper          mapper)
    {
        _matches = matches;
        _clubs   = clubs;
        _mapper  = mapper;
    }

    /// <summary>
    /// Match calendar — optionally filtered by club.
    /// </summary>
    public async Task<IActionResult> Index(int? clubId)
    {
        var matchEntities = clubId.HasValue
            ? await _matches.GetByClubAsync(clubId.Value)
            : await _matches.GetAllWithClubsAsync();

        var allClubs = await _clubs.GetAllAsync();

        var vm = new MatchListVM
        {
            Matches    = _mapper.Map<IEnumerable<MatchListItemVM>>(matchEntities),
            Clubs      = allClubs.Select(c => c.Name),
            FilterClub = clubId.HasValue
                ? allClubs.FirstOrDefault(c => c.Id == clubId)?.Name
                : null
        };

        return View(vm);
    }

    /// <summary>
    /// Match detail page: sector availability grid + add-to-cart controls.
    ///
    /// IMPORTANT: queries are run sequentially (not with Task.WhenAll) because
    /// EF Core DbContext is NOT thread-safe — running multiple async queries on
    /// the same context instance in parallel throws an InvalidOperationException.
    /// </summary>
    public async Task<IActionResult> Detail(int id)
    {
        // Load match with club navigation
        var allMatches = await _matches.GetAllWithClubsAsync();
        var match      = allMatches.FirstOrDefault(m => m.Id == id);
        if (match is null) return NotFound();

        // Load home club with its stadium + sectors
        var club = await _clubs.GetWithStadiumAndSectorsAsync(match.HomeClubId);
        var sectors = club?.Stadium?.Sectors?.ToList()
                      ?? new List<ChampionsLeague.Domain.Entities.Sector>();

        // Build sector availability — sequential awaits to avoid EF Core concurrency error
        var sectorVms = new List<SectorAvailabilityVM>();
        foreach (var sec in sectors)
        {
            int sold = 0;
            try
            {
                sold = await _matches.GetSoldCountAsync(id, sec.Id);
            }
            catch
            {
                // If count fails, treat as 0 sold — don't crash the whole page
                sold = 0;
            }

            sectorVms.Add(new SectorAvailabilityVM
            {
                SectorId   = sec.Id,
                SectorName = sec.Name,
                Capacity   = sec.Capacity,
                Available  = Math.Max(0, sec.Capacity - sold),
                Price      = sec.BasePrice
            });
        }

        var vm = new MatchDetailVM
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

        return View(vm);
    }
}
