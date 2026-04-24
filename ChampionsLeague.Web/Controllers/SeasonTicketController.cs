using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Services;
using ChampionsLeague.Services.DTOs;
using ChampionsLeague.Web.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Abonnements-pagina.
///
/// FIX: SeasonTicket/Index.cshtml verwacht
/// @model IEnumerable&lt;ChampionsLeague.Domain.Entities.Club&gt;
/// en gebruikt rechtstreeks club.PrimaryColor, club.Stadium?.Sectors, etc.
/// De controller geeft nu de ruwe Club-entiteiten terug via
/// IClubService.GetAllEntitiesWithStadiumsAsync() — de view hoeft niet
/// aangepast te worden.
///
/// Voor de Purchase-actie wordt IClubService.GetAllWithStadiumsAsync()
/// (DTOs) gebruikt omdat daar enkel naam en sectorId nodig zijn.
/// </summary>
public class SeasonTicketController : Controller
{
    private readonly IClubService         _clubService;
    private readonly ISeasonTicketService _seasonTicketService;
    private readonly TranslationService   _tr;

    private const string CartSessionKey = "CART";
    private static readonly DateTime CompetitionStart = new DateTime(2026, 4, 25);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase
    };

    public SeasonTicketController(
        IClubService         clubService,
        ISeasonTicketService seasonTicketService,
        TranslationService   tr)
    {
        _clubService         = clubService;
        _seasonTicketService = seasonTicketService;
        _tr                  = tr;
    }

    /// <summary>
    /// Toont de abonnements-pagina.
    /// Geeft ruwe Club-entiteiten door zodat de bestaande view ongewijzigd blijft.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        if (DateTime.UtcNow >= CompetitionStart)
        {
            ViewBag.Closed = true;
            return View((IEnumerable<Club>?)null);
        }

        // Ruwe entiteiten — view verwacht IEnumerable<Club>
        var clubs = await _clubService.GetAllEntitiesWithStadiumsAsync();
        return View(clubs);
    }

    /// <summary>
    /// Voegt een seizoensabonnement toe aan de winkelwagen.
    /// Gebruikt DTOs voor de capaciteits- en max-4-controle.
    /// </summary>
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

        // DTOs voor de Purchase-logica
        var clubDtos = await _clubService.GetAllWithStadiumsAsync();
        var club     = clubDtos.FirstOrDefault(c => c.Sectors.Any(s => s.Id == sectorId));
        var sector   = club?.Sectors.FirstOrDefault(s => s.Id == sectorId);

        if (sector is null || club is null)
        {
            TempData["Error"] = "Sector niet gevonden.";
            return RedirectToAction(nameof(Index));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cart   = GetCart();

        // Max-4-per-club controle
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
        return JsonSerializer.Deserialize<CartVM>(json, _jsonOptions) ?? new CartVM();
    }

    private void SaveCart(CartVM cart)
        => HttpContext.Session.SetString(CartSessionKey,
               JsonSerializer.Serialize(cart, _jsonOptions));
}
