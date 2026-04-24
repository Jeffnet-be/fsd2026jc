using ChampionsLeague.Services;
using ChampionsLeague.Services.DTOs;
using ChampionsLeague.Web.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Abonnements-pagina. Gebruikt dezelfde ClubCardVM als de homepagina
/// zodat _ClubCard.cshtml hergebruikt kan worden.
/// Alle ClubDto-velden (PrimaryColor, Country, TotalCapacity) worden doorgegeven.
/// </summary>
public class SeasonTicketController : Controller
{
    private readonly IClubService         _clubService;
    private readonly ISeasonTicketService _seasonTicketService;
    private readonly TranslationService   _tr;

    private const string CartSessionKey = "CART";
    private static readonly DateTime CompetitionStart = new DateTime(2026, 4, 25);

    public SeasonTicketController(
        IClubService         clubService,
        ISeasonTicketService seasonTicketService,
        TranslationService   tr)
    {
        _clubService         = clubService;
        _seasonTicketService = seasonTicketService;
        _tr                  = tr;
    }

    public async Task<IActionResult> Index()
    {
        if (DateTime.UtcNow >= CompetitionStart)
        {
            ViewBag.Closed = true;
            return View();
        }

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
        }).ToList();

        return View(vms);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Purchase(int sectorId, string totalPrice)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToAction("Login", "Account",
                new { returnUrl = Url.Action("Index", "SeasonTicket") });

        if (DateTime.UtcNow >= CompetitionStart)
        {
            TempData["Error"] = "Season tickets can no longer be purchased.";
            return RedirectToAction(nameof(Index));
        }

        decimal price = decimal.Parse(totalPrice,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture);

        var clubs  = await _clubService.GetAllWithStadiumsAsync();
        var club   = clubs.FirstOrDefault(c => c.Sectors.Any(s => s.Id == sectorId));
        var sector = club?.Sectors.FirstOrDefault(s => s.Id == sectorId);

        if (sector is null || club is null)
        {
            TempData["Error"] = "Sector niet gevonden.";
            return RedirectToAction(nameof(Index));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cart   = GetCart();

        var sectorIdsForClub = club.Sectors.Select(s => s.Id);
        var countInDb        = await _seasonTicketService.CountActiveForClubAsync(userId, sectorIdsForClub);
        var countInCart      = cart.SeasonItems.Count(i => sectorIdsForClub.Contains(i.SectorId));

        if (countInDb + countInCart >= 4)
        {
            TempData["Error"] = $"Maximum 4 abonnementen per club ({club.Name}).";
            return RedirectToAction(nameof(Index));
        }

        cart.SeasonItems.Add(new SeasonCartItemVM
        {
            SectorId    = sectorId,
            SectorName  = sector.Name,
            ClubName    = club.Name,
            StadiumName = club.StadiumName,
            TotalPrice  = price
        });

        SaveCart(cart);
        TempData["Success"] = _tr.T("season_added_to_cart");
        return RedirectToAction(nameof(Index));
    }

    private CartVM GetCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json)) return new CartVM();
        return JsonSerializer.Deserialize<CartVM>(json) ?? new CartVM();
    }

    private void SaveCart(CartVM cart)
        => HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
}
