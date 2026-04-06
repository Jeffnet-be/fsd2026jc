using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Services;
using ChampionsLeague.Web.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ChampionsLeague.Web.Controllers;

[Authorize]
public class SeasonTicketController : Controller
{
    private readonly IClubRepository             _clubs;
    private readonly ISeasonTicketRepository     _seasonTickets;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TranslationService          _tr;
    private readonly IEmailService               _email;

    private const string CartSessionKey = "CART";

    private static readonly DateTime CompetitionStart =
        new DateTime(2026, 4, 22, 0, 0, 0, DateTimeKind.Utc);

    public SeasonTicketController(
        IClubRepository              clubs,
        ISeasonTicketRepository      seasonTickets,
        UserManager<ApplicationUser> userManager,
        TranslationService           tr,
        IEmailService                email)
    {
        _clubs         = clubs;
        _seasonTickets = seasonTickets;
        _userManager   = userManager;
        _tr            = tr;
        _email         = email;
    }

    public async Task<IActionResult> Index()
    {
        if (DateTime.UtcNow >= CompetitionStart)
        {
            ViewBag.Closed = true;
            return View();
        }
        var clubs = await _clubs.GetAllWithStadiumsAsync();
        return View(clubs);
    }

    /// <summary>
    /// Adds a season ticket to the cart — does NOT save to DB yet.
    /// The actual seat number is assigned and saved during checkout,
    /// preventing seat numbers from incrementing on repeated clicks.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int sectorId, decimal totalPrice)
    {
        if (DateTime.UtcNow >= CompetitionStart)
        {
            TempData["Error"] = _tr.T("season_err_started");
            return RedirectToAction(nameof(Index));
        }

        // Find sector and club info for display in the cart
        var clubs      = await _clubs.GetAllWithStadiumsAsync();
        var club       = clubs.FirstOrDefault(c =>
            c.Stadium?.Sectors.Any(s => s.Id == sectorId) == true);
        var sector     = club?.Stadium?.Sectors.FirstOrDefault(s => s.Id == sectorId);

        if (sector is null || club is null)
        {
            TempData["Error"] = "Sector not found.";
            return RedirectToAction(nameof(Index));
        }

        // Check if already in cart (one season ticket per sector per person)
        var cart = GetCart();
        if (cart.SeasonItems.Any(i => i.SectorId == sectorId))
        {
            TempData["Error"] = _tr.T("season_err_already_in_cart");
            return RedirectToAction(nameof(Index));
        }

        cart.SeasonItems.Add(new SeasonCartItemVM
        {
            SectorId    = sectorId,
            SectorName  = sector.Name,
            StadiumName = club.Stadium?.Name ?? "",
            ClubName    = club.Name,
            TotalPrice  = totalPrice
        });

        SaveCart(cart);
        TempData["Success"] = _tr.T("season_added_to_cart");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Called by CheckoutController after payment is confirmed.
    /// Assigns the actual seat number and saves the season ticket to DB.
    /// Sends confirmation email only after successful save.
    /// </summary>
    public async Task<(bool Success, string? Error, int SeatNumber)> FinalizeSeasonTicketAsync(
        string userId, SeasonCartItemVM item)
    {
        var reserved = (await _seasonTickets.GetSeasonReservedSeatsAsync(item.SectorId)).ToHashSet();
        var nextSeat = Enumerable.Range(1, 1000).FirstOrDefault(s => !reserved.Contains(s));

        if (nextSeat == 0)
            return (false, _tr.T("season_err_full"), 0);

        var seasonTicket = new SeasonTicket
        {
            UserId      = userId,
            SectorId    = item.SectorId,
            SeatNumber  = nextSeat,
            TotalPrice  = item.TotalPrice,
            PurchasedAt = DateTime.UtcNow,
            IsActive    = true
        };

        await _seasonTickets.AddAsync(seasonTicket);
        await _seasonTickets.SaveChangesAsync();

        return (true, null, nextSeat);
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
