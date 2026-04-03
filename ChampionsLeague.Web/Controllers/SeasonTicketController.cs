using AutoMapper;
using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Season ticket purchase — business rule: can only be bought before competition start.
/// An abonnement-seat cannot then be sold as a single ticket (enforced in TicketService).
/// </summary>
[Authorize]
public class SeasonTicketController : Controller
{
    private readonly IClubRepository          _clubs;
    private readonly ISeasonTicketRepository  _seasonTickets;
    private readonly UserManager<ApplicationUser> _userManager;

    public SeasonTicketController(
        IClubRepository         clubs,
        ISeasonTicketRepository seasonTickets,
        UserManager<ApplicationUser> userManager)
    {
        _clubs         = clubs;
        _seasonTickets = seasonTickets;
        _userManager   = userManager;
    }

    // Competition start date — season tickets only available before this date
    private static readonly DateTime CompetitionStart = new DateTime(2026, 4, 22); // First match date

    /// <summary>GET /SeasonTicket — shows available clubs/sectors for season ticket purchase.</summary>
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

    /// <summary>POST /SeasonTicket/Purchase — purchases a season ticket for a sector.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Purchase(int sectorId, decimal totalPrice)
    {
        if (DateTime.UtcNow >= CompetitionStart)
        {
            TempData["Error"] = "Season tickets can no longer be purchased — the competition has started.";
            return RedirectToAction(nameof(Index));
        }

        var userId = _userManager.GetUserId(User)!;

        // Find next available seat in this sector (simple sequential allocation)
        var reserved = (await _seasonTickets.GetSeasonReservedSeatsAsync(sectorId)).ToHashSet();
        var nextSeat = Enumerable.Range(1, 1000).FirstOrDefault(s => !reserved.Contains(s));

        if (nextSeat == 0)
        {
            TempData["Error"] = "No more season ticket seats available in this sector.";
            return RedirectToAction(nameof(Index));
        }

        var seasonTicket = new SeasonTicket
        {
            UserId      = userId,
            SectorId    = sectorId,
            SeatNumber  = nextSeat,
            TotalPrice  = totalPrice,
            PurchasedAt = DateTime.UtcNow,
            IsActive    = true
        };

        await _seasonTickets.AddAsync(seasonTicket);
        await _seasonTickets.SaveChangesAsync();

        TempData["Success"] = $"Season ticket purchased! Your seat number is {nextSeat}.";
        return RedirectToAction(nameof(Index));
    }
}
