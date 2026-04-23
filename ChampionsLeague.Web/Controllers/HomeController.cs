using AutoMapper;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Serves the public introduction page with a grid of club cards.
/// No authentication required — accessible to anonymous visitors.
/// AutoMapper maps Club entities to ClubCardVM so views never touch domain objects directly.
/// </summary>
public class HomeController : Controller
{
    private readonly IClubRepository _clubs;
    private readonly IMapper         _mapper;

    /// <summary>
    /// Dependencies injected by the DI container.
    /// </summary>
    public HomeController(IClubRepository clubs, IMapper mapper)
    {
        _clubs  = clubs;
        _mapper = mapper;
    }

    /// <summary>
    /// Index: loads all six clubs with stadiums and sectors,
    /// maps to ClubCardVM via AutoMapper, returns the view.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var clubs = await _clubs.GetAllWithStadiumsAsync();
        var vms   = _mapper.Map<IEnumerable<ClubCardVM>>(clubs);
        return View(vms);
    }

    /// <summary>Standard error page — required by the ASP.NET Core project template.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
