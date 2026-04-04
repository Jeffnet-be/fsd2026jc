using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Services;
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
    private readonly IEmailService               _email;

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
        if (DateTime.UtcNow >= CompetitionStart)
        {
            TempData["Error"] = _tr.T("season_err_started");
            return RedirectToAction(nameof(Index));
        }

        var user     = await _userManager.GetUserAsync(User)!;
        var userId   = user!.Id;
        var reserved = (await _seasonTickets.GetSeasonReservedSeatsAsync(sectorId)).ToHashSet();
        var nextSeat = Enumerable.Range(1, 1000).FirstOrDefault(s => !reserved.Contains(s));

        if (nextSeat == 0)
        {
            TempData["Error"] = _tr.T("season_err_full");
            return RedirectToAction(nameof(Index));
        }

        // Find the sector name for the email
        var clubs     = await _clubs.GetAllWithStadiumsAsync();
        var sector    = clubs.SelectMany(c => c.Stadium?.Sectors ?? Enumerable.Empty<Sector>())
                             .FirstOrDefault(s => s.Id == sectorId);
        var sectorName = sector?.Name ?? "Unknown sector";
        var stadiumName = clubs.FirstOrDefault(c =>
            c.Stadium?.Sectors.Any(s => s.Id == sectorId) == true)?.Stadium?.Name ?? "";

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

        // ── Send season ticket confirmation email ─────────────────────
        await _email.SendAsync(
            to      : user.Email!,
            subject : _tr.T("season_email_subject"),
            htmlBody: $@"
<p>Hello {user.FirstName},</p>
<p>{_tr.T("season_email_intro")}</p>
<table style='border-collapse:collapse;font-family:Arial,sans-serif'>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>{_tr.T("tickets_col_sector")}:</td>
      <td style='padding:6px 0;font-weight:bold'>{sectorName}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>{_tr.T("tickets_col_seat")}:</td>
      <td style='padding:6px 0;font-weight:bold'>{nextSeat}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>{_tr.T("season_price")}:</td>
      <td style='padding:6px 0;font-weight:bold'>€ {totalPrice:0.00}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>{_tr.T("match_col_stadium")}:</td>
      <td style='padding:6px 0;font-weight:bold'>{stadiumName}</td></tr>
</table>
<p>{_tr.T("season_email_footer")}</p>
<p>CL Tickets Portal</p>"
        );

        TempData["Success"] = $"{_tr.T("season_success")} {nextSeat}.";
        return RedirectToAction(nameof(Index));
    }
}
