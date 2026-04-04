using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

[Authorize]
public class SeasonTicketController : Controller
{
    private readonly IClubRepository             _clubs;
    private readonly ISeasonTicketRepository     _seasonTickets;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TranslationService          _tr;

    public SeasonTicketController(
        IClubRepository              clubs,
        ISeasonTicketRepository      seasonTickets,
        UserManager<ApplicationUser> userManager,
        TranslationService           tr)
    {
        _clubs         = clubs;
        _seasonTickets = seasonTickets;
        _userManager   = userManager;
        _tr            = tr;
    }

    // Competition start = first match date — season tickets only sold BEFORE this
    private static readonly DateTime CompetitionStart = new DateTime(2026, 4, 22, 0, 0, 0, DateTimeKind.Utc);

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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Purchase(int sectorId, decimal totalPrice)
    {
        // Double-check rule server-side even if form was somehow submitted
        if (DateTime.UtcNow >= CompetitionStart)
        {
            TempData["Error"] = _tr.T("season_err_started");
            return RedirectToAction(nameof(Index));
        }

        var userId   = _userManager.GetUserId(User)!;
        var reserved = (await _seasonTickets.GetSeasonReservedSeatsAsync(sectorId)).ToHashSet();
        var nextSeat = Enumerable.Range(1, 1000).FirstOrDefault(s => !reserved.Contains(s));

        if (nextSeat == 0)
        {
            TempData["Error"] = _tr.T("season_err_full");
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

        // Translated success message with seat number
        TempData["Success"] = $"{_tr.T("season_success")} {nextSeat}.";
        return RedirectToAction(nameof(Index));
    }
}
