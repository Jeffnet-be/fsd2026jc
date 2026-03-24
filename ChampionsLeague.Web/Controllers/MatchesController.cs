using AutoMapper;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Displays the match calendar with optional club filter.
/// The Index view uses jQuery DataTables (curriculum section 10.4.3) for
/// client-side ordering, searching, and paging — no extra controller actions needed.
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
    /// Returns all matches (or filtered by clubId) mapped to MatchListItemVM.
    /// The optional clubId query-string parameter drives the LINQ Where clause.
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
    /// Detail page for one match: sector availability grid + add-to-cart controls.
    /// Runs GetSoldCountAsync per sector in parallel with Task.WhenAll for efficiency.
    /// </summary>
    public async Task<IActionResult> Detail(int id)
    {
        var allMatches = await _matches.GetAllWithClubsAsync();
        var match      = allMatches.FirstOrDefault(m => m.Id == id);
        if (match is null) return NotFound();

        var club = await _clubs.GetWithStadiumAndSectorsAsync(match.HomeClubId);
        var sectors = club?.Stadium?.Sectors ?? Enumerable.Empty<ChampionsLeague.Domain.Entities.Sector>();

        // Parallelise the sold-count queries with Task.WhenAll
        var sectorTasks = sectors.Select(async sec =>
        {
            var sold = await _matches.GetSoldCountAsync(id, sec.Id);
            return new SectorAvailabilityVM
            {
                SectorId   = sec.Id,
                SectorName = sec.Name,
                Capacity   = sec.Capacity,
                Available  = Math.Max(0, sec.Capacity - sold),
                Price      = sec.BasePrice
            };
        });

        var resolvedSectors = await Task.WhenAll(sectorTasks);

        var vm = new MatchDetailVM
        {
            Id            = match.Id,
            HomeClubName  = match.HomeClub.Name,
            AwayClubName  = match.AwayClub.Name,
            HomeClubBadge = match.HomeClub.BadgeUrl,
            AwayClubBadge = match.AwayClub.BadgeUrl,
            MatchDate     = match.MatchDate,
            Phase         = match.Phase,
            StadiumName   = match.HomeClub.Stadium?.Name ?? string.Empty,
            StadiumCity   = match.HomeClub.Stadium?.City ?? string.Empty,
            IsSaleOpen    = match.IsSaleOpen,
            Sectors       = resolvedSectors.ToList()
        };

        return View(vm);
    }
}
