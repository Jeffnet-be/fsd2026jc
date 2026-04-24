// ════════════════════════════════════════════════════════════════════════
// HomeController.cs
// ════════════════════════════════════════════════════════════════════════
using ChampionsLeague.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Startpagina. Haalt ClubDtos op via IClubService, mapt naar ClubCardVM hier in Web.
/// AutoMapper kan ook gebruikt worden — beide zijn correct zolang de mapping in Web zit.
/// </summary>
public class HomeController : Controller
{
    private readonly IClubService _clubService;

    public HomeController(IClubService clubService)
    {
        _clubService = clubService;
    }

    public async Task<IActionResult> Index()
    {
        var dtos = await _clubService.GetAllWithStadiumsAsync();

        // Mapping DTO → ViewModel gebeurt in de Web-laag (correct)
        var vms = dtos.Select(d => new ClubCardVM
        {
            Id          = d.Id,
            Name        = d.Name,
            BadgeUrl    = d.BadgeUrl,
            StadiumName = d.StadiumName,
            StadiumCity = d.StadiumCity,
            Sectors     = d.Sectors.Select(s => new SectorCardVM
            {
                Id        = s.Id,
                Name      = s.Name,
                Capacity  = s.Capacity,
                BasePrice = s.BasePrice
            }).ToList()
        });

        return View(vms);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
