using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ChampionsLeague.Web.Controllers;

public class SeasonTicketController : Controller
{
    private readonly IClubRepository _clubs;
    private readonly ISeasonTicketRepository _seasonTickets;
    private readonly UserManager<ApplicationUser> _userManager;

    private const string CartSessionKey = "CART";

    private static readonly DateTime CompetitionStart = new DateTime(2026, 4, 25);

    public SeasonTicketController(
        IClubRepository clubs,
        ISeasonTicketRepository seasonTickets,
        UserManager<ApplicationUser> userManager)
    {
        _clubs = clubs;
        _seasonTickets = seasonTickets;
        _userManager = userManager;
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
    /// Adds the season ticket to the SESSION CART only.
    /// No DB write happens here — seat assignment happens at checkout.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Purchase(int sectorId, decimal totalPrice)
    {
        if (DateTime.UtcNow >= CompetitionStart)
        {
            TempData["Error"] = "Season tickets can no longer be purchased — the competition has started.";
            return RedirectToAction(nameof(Index));
        }

        // Get club/sector info for display in the cart
        var clubs = await _clubs.GetAllWithStadiumsAsync();
        var club = clubs.FirstOrDefault(c => c.Stadium?.Sectors.Any(s => s.Id == sectorId) == true);
        var sector = club?.Stadium?.Sectors.FirstOrDefault(s => s.Id == sectorId);

        if (sector is null || club is null)
        {
            TempData["Error"] = "Sector not found.";
            return RedirectToAction(nameof(Index));
        }

        // Load existing cart from session
        var cart = GetCart();

        // Prevent duplicate season ticket for same sector
        if (cart.SeasonItems.Any(i => i.SectorId == sectorId))
        {
            TempData["Error"] = "You already have a season ticket for this sector in your cart.";
            return RedirectToAction(nameof(Index));
        }

        // Add to cart — NO seat number yet, NO DB write yet
        cart.SeasonItems.Add(new SeasonCartItemVM
        {
            SectorId = sectorId,
            SectorName = sector.Name,
            ClubName = club.Name,
            StadiumName = club.Stadium?.Name ?? "",
            TotalPrice = totalPrice
        });

        SaveCart(cart);

        TempData["Success"] = "Season ticket added to your cart!";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Called by CheckoutController AFTER payment confirmation.
    /// This is where the seat number is assigned and saved to DB.
    /// </summary>
    public async Task<(bool Success, string? Error, int SeatNumber)> FinalizeAsync(
        string userId, SeasonCartItemVM item)
    {
        var reserved = (await _seasonTickets.GetSeasonReservedSeatsAsync(item.SectorId)).ToHashSet();
        var nextSeat = Enumerable.Range(1, 1000).FirstOrDefault(s => !reserved.Contains(s));

        if (nextSeat == 0)
            return (false, "No seats available in this sector.", 0);

        var seasonTicket = new SeasonTicket
        {
            UserId = userId,
            SectorId = item.SectorId,
            SeatNumber = nextSeat,
            TotalPrice = item.TotalPrice,
            PurchasedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _seasonTickets.AddAsync(seasonTicket);
        await _seasonTickets.SaveChangesAsync();

        return (true, null, nextSeat);
    }

    // ── Session helpers ───────────────────────────────────────────────
    private CartVM GetCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json)) return new CartVM();
        return JsonSerializer.Deserialize<CartVM>(json) ?? new CartVM();
    }

    private void SaveCart(CartVM cart)
        => HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
}