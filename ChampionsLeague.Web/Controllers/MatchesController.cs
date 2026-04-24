using AutoMapper;
using ChampionsLeague.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Wedstrijdkalender en wedstrijd-detailpagina.
///
/// REFACTOR: IMatchRepository + IClubRepository vervangen door IMatchService.
/// Vroeger bouwde deze controller zelf het MatchDetailVM op via LINQ en meerdere
/// repository-calls. Die logica zit nu in MatchService.GetDetailAsync().
/// De controller doet alleen nog: request ontvangen → service aanroepen → view teruggeven.
///
/// Voordeel: als de manier waarop MatchDetailVM samengesteld wordt verandert,
/// hoef je enkel MatchService aan te passen — de controller blijft ongewijzigd.
/// </summary>
public class MatchesController : Controller
{
    private readonly IMatchService _matchService;
    private readonly IClubService  _clubService;
    private readonly IMapper       _mapper;

    public MatchesController(IMatchService matchService, IClubService clubService, IMapper mapper)
    {
        _matchService = matchService;
        _clubService  = clubService;
        _mapper       = mapper;
    }

    /// <summary>
    /// Wedstrijdkalender — optioneel gefilterd op club.
    /// </summary>
    public async Task<IActionResult> Index(int? clubId)
    {
        var matchEntities = clubId.HasValue
            ? await _matchService.GetByClubAsync(clubId.Value)
            : await _matchService.GetAllWithClubsAsync();

        var allClubs = await _clubService.GetAllAsync();

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
    /// Wedstrijd-detailpagina met sector-beschikbaarheid.
    ///
    /// REFACTOR: de volledige opbouw van MatchDetailVM zit nu in IMatchService.GetDetailAsync().
    /// Deze controller bevat geen LINQ of repository-calls meer.
    /// </summary>
    public async Task<IActionResult> Detail(int id)
    {
        var vm = await _matchService.GetDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }
}
