using ChampionsLeague.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Startpagina — toont alle clubs als kaarten via _ClubCard.cshtml partial.
/// Mapt ClubDto naar ClubCardVM inclusief PrimaryColor, Country en TotalCapacity
/// die de view nodig heeft voor de styling en het badge-tellen.
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

        var vms = dtos.Select(d => new ClubCardVM
        {
            Id            = d.Id,
            Name          = d.Name,
            Country       = d.Country,
            BadgeUrl      = d.BadgeUrl,
            PrimaryColor  = d.PrimaryColor,
            StadiumName   = d.StadiumName,
            StadiumCity   = d.StadiumCity,
            TotalCapacity = d.TotalCapacity,
            Sectors       = d.Sectors.Select(s => new SectorVM
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
