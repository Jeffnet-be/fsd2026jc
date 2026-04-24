using AutoMapper;
using ChampionsLeague.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Startpagina van het portaal — grid van clubkaarten.
///
/// REFACTOR: injecteert nu <see cref="IMatchService"/> in plaats van
/// <see cref="ChampionsLeague.Domain.Interfaces.IClubRepository"/> rechtstreeks.
/// Het Web-project heeft zo geen directe afhankelijkheid meer op de Infrastructure-laag
/// voor deze controller.
///
/// Afhankelijkheidsgraph na de fix:
///   HomeController → IMatchService (Services) → IClubRepository (Infrastructure)
/// </summary>
public class HomeController : Controller
{
    private readonly IMatchService _matchService;
    private readonly IMapper       _mapper;

    public HomeController(IMatchService matchService, IMapper mapper)
    {
        _matchService = matchService;
        _mapper       = mapper;
    }

    /// <summary>
    /// Laadt alle zes clubs met hun stadion en sectoren via de service-laag.
    /// AutoMapper zet Club-entiteiten om naar ClubCardVM zodat de view
    /// nooit rechtstreeks met domeinobjecten werkt.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var clubs = await _matchService.GetAllClubsWithStadiumsAsync();
        var vms   = _mapper.Map<IEnumerable<ClubCardVM>>(clubs);
        return View(vms);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
