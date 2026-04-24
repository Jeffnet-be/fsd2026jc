using ChampionsLeague.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Wedstrijdkalender en detail.
/// Haalt MatchDto / MatchDetailDto op via IMatchService.
/// Mapt DTOs naar ViewModels hier in de Web-laag — services kennen geen ViewModels.
/// </summary>
public class MatchesController : Controller
{
    private readonly IMatchService _matchService;
    private readonly IClubService  _clubService;

    public MatchesController(IMatchService matchService, IClubService clubService)
    {
        _matchService = matchService;
        _clubService  = clubService;
    }

    public async Task<IActionResult> Index(int? clubId)
    {
        var matchDtos = clubId.HasValue
            ? await _matchService.GetByClubAsync(clubId.Value)
            : await _matchService.GetAllAsync();

        var clubDtos = await _clubService.GetAllAsync();

        // Mapping DTO → ViewModel in Web-laag
        var vm = new MatchListVM
        {
            Matches = matchDtos.Select(d => new MatchListItemVM
            {
                Id            = d.Id,
                HomeClubName  = d.HomeClubName,
                AwayClubName  = d.AwayClubName,
                HomeClubBadge = d.HomeClubBadge,
                AwayClubBadge = d.AwayClubBadge,
                StadiumName   = d.StadiumName,
                StadiumCity   = d.StadiumCity,
                MatchDate     = d.MatchDate,
                Phase         = d.Phase,
                IsSaleOpen    = d.IsSaleOpen
            }),
            Clubs      = clubDtos.Select(c => c.Name),
            FilterClub = clubId.HasValue
                ? clubDtos.FirstOrDefault(c => c.Id == clubId)?.Name
                : null
        };

        return View(vm);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var dto = await _matchService.GetDetailAsync(id);
        if (dto is null) return NotFound();

        // Mapping DTO → ViewModel
        var vm = new MatchDetailVM
        {
            Id            = dto.Match.Id,
            HomeClubName  = dto.Match.HomeClubName,
            AwayClubName  = dto.Match.AwayClubName,
            HomeClubBadge = dto.Match.HomeClubBadge,
            AwayClubBadge = dto.Match.AwayClubBadge,
            MatchDate     = dto.Match.MatchDate,
            Phase         = dto.Match.Phase,
            StadiumName   = dto.Match.StadiumName,
            StadiumCity   = dto.Match.StadiumCity,
            IsSaleOpen    = dto.Match.IsSaleOpen,
            Sectors       = dto.Sectors.Select(s => new SectorAvailabilityVM
            {
                SectorId   = s.SectorId,
                SectorName = s.SectorName,
                Capacity   = s.Capacity,
                Available  = s.Available,
                Price      = s.Price
            }).ToList()
        };

        return View(vm);
    }
}
