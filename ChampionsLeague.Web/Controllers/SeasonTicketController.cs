using ChampionsLeague.Services;
using ChampionsLeague.Web.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ChampionsLeague.Domain.Entities;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Abonnementen-pagina: overzicht en toevoegen aan winkelwagen.
///
/// REFACTOR: IClubRepository en ISeasonTicketRepository vervangen door
/// IClubService en ISeasonTicketService.
/// De controller bevat geen directe repository-aanroepen meer.
///
/// WINKELWAGEN-BUGS: Verklaring waarom de originele winkelwagen minder problemen had:
/// De cart-logica hier gebruikt (MatchId, SectorId) als samengestelde sleutel voor
/// Remove(). Seizoensabonnementen hebben geen MatchId, maar wel SectorId als sleutel.
/// Het "hele wagen leeg"-probleem trad op bij bepaalde UI-flows, niet bij alle removes.
/// In de meegegeven code is de Remove-logica correct per (MatchId, SectorId) —
/// het probleem zat eerder in hoe de knop het form submitde in de view.
/// </summary>
public class SeasonTicketController : Controller
{
    private readonly IClubService                         _clubService;
    private readonly ISeasonTicketService                 _seasonTicketService;
    private readonly UserManager<ApplicationUser>         _userManager;
    private readonly TranslationService                   _tr;

    private const string CartSessionKey = "CART";
    private static readonly DateTime CompetitionStart = new DateTime(2026, 4, 25);

    public SeasonTicketController(
        IClubService                         clubService,
        ISeasonTicketService                 seasonTicketService,
        UserManager<ApplicationUser>         userManager,
        TranslationService                   tr)
    {
        _clubService         = clubService;
        _seasonTicketService = seasonTicketService;
        _userManager         = userManager;
        _tr                  = tr;
    }

    /// <summary>
    /// Toont de abonnements-pagina met alle clubs en beschikbare sectoren.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        if (DateTime.UtcNow >= CompetitionStart)
        {
            ViewBag.Closed = true;
            return View();
        }

        var clubs = await _clubService.GetAllWithStadiumsAsync();
        return View(clubs);
    }

    /// <summary>
    /// Voegt een seizoensabonnement toe aan de sessie-winkelwagen.
    /// Geen DB-write hier — dat gebeurt bij checkout (Checkout/Confirm).
    ///
    /// De max-4-per-club controle gebruikt ISeasonTicketService, niet de repository.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Purchase(int sectorId, string totalPrice)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToAction("Login", "Account",
                new { returnUrl = Url.Action("Index", "SeasonTicket") });

        if (DateTime.UtcNow >= CompetitionStart)
        {
            TempData["Error"] = "Season tickets can no longer be purchased — the competition has started.";
            return RedirectToAction(nameof(Index));
        }

        decimal price = decimal.Parse(totalPrice,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture);

        var clubs  = await _clubService.GetAllWithStadiumsAsync();
        var club   = clubs.FirstOrDefault(c => c.Stadium?.Sectors.Any(s => s.Id == sectorId) == true);
        var sector = club?.Stadium?.Sectors.FirstOrDefault(s => s.Id == sectorId);

        if (sector is null || club is null)
        {
            TempData["Error"] = "Sector not found.";
            return RedirectToAction(nameof(Index));
        }

        var userId = _userManager.GetUserId(User)!;
        var cart   = GetCart();

        // Max-4-per-club: DB-teller via service + cart-teller samen
        var sectorIdsForClub = club.Stadium!.Sectors.Select(s => s.Id);
        var countInDb   = await _seasonTicketService.CountActiveForClubAsync(userId, sectorIdsForClub);
        var countInCart = cart.SeasonItems.Count(i => sectorIdsForClub.Contains(i.SectorId));

        if (countInDb + countInCart >= 4)
        {
            TempData["Error"] = $"U kunt maximaal 4 abonnementen kopen voor {club.Name}.";
            return RedirectToAction(nameof(Index));
        }

        cart.SeasonItems.Add(new SeasonCartItemVM
        {
            SectorId    = sectorId,
            SectorName  = sector.Name,
            ClubName    = club.Name,
            StadiumName = club.Stadium?.Name ?? "",
            TotalPrice  = price
        });

        SaveCart(cart);
        TempData["Success"] = _tr.T("season_added_to_cart");
        return RedirectToAction(nameof(Index));
    }

    // ── Session helpers ─────────────────────────────────────────────────
    private CartVM GetCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json)) return new CartVM();
        return JsonSerializer.Deserialize<CartVM>(json) ?? new CartVM();
    }

    private void SaveCart(CartVM cart)
        => HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
}
