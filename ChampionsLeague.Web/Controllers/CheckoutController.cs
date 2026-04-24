using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Infrastructure.Services;
using ChampionsLeague.Services;
using ChampionsLeague.Web.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Verwerkt de afrekening van de winkelwagen.
///
/// REFACTOR: IMatchRepository en ISeasonTicketRepository vervangen door
/// IMatchService en ISeasonTicketService.
/// De controller injecteert enkel service-interfaces — geen repositories.
///
/// OVERBOEKING FIX: De aankoop van abonnementen in Confirm() verloopt nu via
/// ISeasonTicketService.FinalizeAsync() dat de capaciteitscontrole uitvoert
/// inclusief de check op reeds bezette stoelen door losse tickets.
///
/// ANNULATIE-BUG FIX: Zit in SeasonTicketService (IsActive = false) en
/// in TicketService (Status = Cancelled) — de query filtert al op die velden.
/// </summary>
[Authorize]
public class CheckoutController : Controller
{
    private const string CartSessionKey = "CART";

    private readonly ITicketService               _ticketService;
    private readonly ISeasonTicketService         _seasonTicketService;
    private readonly IMatchService                _matchService;
    private readonly IEmailService                _email;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TranslationService           _tr;

    public CheckoutController(
        ITicketService               ticketService,
        ISeasonTicketService         seasonTicketService,
        IMatchService                matchService,
        IEmailService                email,
        UserManager<ApplicationUser> userManager,
        TranslationService           tr)
    {
        _ticketService       = ticketService;
        _seasonTicketService = seasonTicketService;
        _matchService        = matchService;
        _email               = email;
        _userManager         = userManager;
        _tr                  = tr;
    }

    /// <summary>Toont de overzichtspagina vóór bevestiging.</summary>
    public IActionResult Review()
    {
        var cart = GetCart();
        if (!cart.Items.Any() && !cart.SeasonItems.Any())
            return RedirectToAction("Index", "Cart");
        return View(cart);
    }

    /// <summary>
    /// Verwerkt de volledige winkelwagen.
    ///
    /// Strategie:
    /// 1. Verwerk elk item. Bij fout: voeg toe aan errors-lijst, ga verder met de rest.
    /// 2. Als er GEEN fouten zijn: leeg de wagen en stuur e-mail.
    /// 3. Als er WEL fouten zijn: stuur de gebruiker terug naar Review met foutmelding.
    ///    Items die succesvol waren zijn dan wel al in de DB — dit is een design-keuze
    ///    (alternatieven: alles-of-niets via transactie, of pre-validatie).
    ///
    /// OVERBOEKING: TicketService.PurchaseAsync() en SeasonTicketService.FinalizeAsync()
    /// doen elk hun eigen capaciteitscontrole op basis van DB-data op het moment van aankoop.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm()
    {
        var cart   = GetCart();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user   = await _userManager.FindByIdAsync(userId);
        var errors = new List<string>();

        var purchasedTickets       = new List<(Ticket ticket, Match match)>();
        var purchasedSeasonTickets = new List<(SeasonTicket ticket, string sectorName, string stadiumName)>();

        // ── Losse tickets ──────────────────────────────────────────────
        foreach (var item in cart.Items)
        {
            var result = await _ticketService.PurchaseAsync(new PurchaseRequest(
                UserId    : userId,
                MatchId   : item.MatchId,
                SectorId  : item.SectorId,
                Quantity  : item.Quantity,
                UnitPrice : item.UnitPrice
            ));

            if (!result.Success)
            {
                errors.Add($"{item.MatchDescription} — {result.ErrorMessage}");
            }
            else if (result.Tickets is not null)
            {
                var allMatches = await _matchService.GetAllWithClubsAsync();
                var match      = allMatches.FirstOrDefault(m => m.Id == item.MatchId);
                if (match is not null)
                    foreach (var t in result.Tickets)
                        purchasedTickets.Add((t, match));
            }
        }

        // ── Seizoensabonnementen — via service (geen directe repo-aanroep) ──
        foreach (var item in cart.SeasonItems)
        {
            var (success, error, created) = await _seasonTicketService.FinalizeAsync(userId, item);

            if (!success || created is null)
            {
                errors.Add($"{item.ClubName} / {item.SectorName} — {error ?? _tr.T("season_err_full")}");
                continue;
            }

            purchasedSeasonTickets.Add((created, item.SectorName, item.StadiumName));
        }

        if (errors.Any())
        {
            TempData["Errors"] = string.Join("|", errors);
            return RedirectToAction(nameof(Review));
        }

        // Alles geslaagd: wagen leegmaken en e-mail sturen
        HttpContext.Session.Remove(CartSessionKey);

        if (user is not null && (purchasedTickets.Any() || purchasedSeasonTickets.Any()))
            await SendConfirmationEmailAsync(user, purchasedTickets, purchasedSeasonTickets);

        TempData["Success"] = _tr.T("checkout_confirmed_msg");
        return RedirectToAction(nameof(Confirmation));
    }

    public IActionResult Confirmation() => View();

    // ── E-mail helper (ongewijzigd t.o.v. origineel) ──────────────────────

    private async Task SendConfirmationEmailAsync(
        ApplicationUser user,
        List<(Ticket ticket, Match match)> tickets,
        List<(SeasonTicket ticket, string sectorName, string stadiumName)> seasonTickets)
    {
        var lang = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
        if (lang is not ("nl" or "fr" or "en")) lang = "nl";

        var (subject, intro, matchLabel, seatLabel, voucherLabel, seasonSection, footer) = lang switch
        {
            "fr" => ("Confirmation de votre commande — CL Tickets",
                     $"Merci, {user.FirstName}! Voici votre récapitulatif:",
                     "Match", "Siège", "Bon", "Abonnements saison",
                     "Présentez ce bon à l'entrée du stade le jour du match."),
            "en" => ("Your order confirmation — CL Tickets",
                     $"Thank you, {user.FirstName}! Here is your order summary:",
                     "Match", "Seat", "Voucher", "Season tickets",
                     "Present your voucher code at the stadium entrance on match day."),
            _ =>   ("Bevestiging van uw bestelling — CL Tickets",
                    $"Bedankt, {user.FirstName}! Hieronder vindt u uw bestelbevestiging:",
                    "Wedstrijd", "Zitplaats", "Voucher", "Seizoensabonnementen",
                    "Toon uw vouchercode aan de ingang van het stadion op wedstrijddag.")
        };

        var ticketRows = "";
        if (tickets.Any())
        {
            var grouped = tickets.GroupBy(p => p.match.Id);
            ticketRows  = string.Join("", grouped.Select(g =>
            {
                var m    = g.First().match;
                var desc = $"{m.HomeClub?.Name} vs {m.AwayClub?.Name}";
                var date = m.MatchDate.ToString("dd MMMM yyyy HH:mm") + " UTC";
                var rows = string.Join("", g.Select(p => $@"
      <tr style='background:#f8f9ff'>
        <td style='padding:6px 16px 6px 24px;color:#444'>{seatLabel} {p.ticket.SeatNumber}</td>
        <td style='padding:6px 8px;font-family:monospace;font-size:12px;color:#001489'>{p.ticket.VoucherId:D}</td>
      </tr>"));
                return $@"
    <tr style='background:#001489'>
      <td colspan='2' style='padding:10px 16px;color:#FFD700;font-weight:bold'>{desc} — {date}</td>
    </tr>{rows}";
            }));
        }

        var seasonRows = "";
        if (seasonTickets.Any())
        {
            seasonRows = $@"
    <tr style='background:#C8A600'>
      <td colspan='2' style='padding:10px 16px;color:#001489;font-weight:bold'>{seasonSection}</td>
    </tr>"
            + string.Join("", seasonTickets.Select(s => $@"
    <tr style='background:#f8f9ff'>
      <td style='padding:8px 16px;color:#444'>{s.stadiumName} — {s.sectorName}</td>
      <td style='padding:8px 8px;font-weight:bold;color:#001489'>{seatLabel} {s.ticket.SeatNumber}</td>
    </tr>"));
        }

        await _email.SendAsync(
            to      : user.Email!,
            subject : subject,
            htmlBody: $@"
<p>{intro}</p>
<table style='border-collapse:collapse;font-family:Arial,sans-serif;width:100%;max-width:580px'>
  <tr style='background:#001489'>
    <th style='padding:10px 16px;color:#FFD700;text-align:left'>{matchLabel}</th>
    <th style='padding:10px 16px;color:#FFD700;text-align:left'>{voucherLabel}</th>
  </tr>
  {ticketRows}
  {seasonRows}
</table>
<p style='margin-top:16px'>{footer}</p>
<p>CL Tickets Portal</p>"
        );
    }

    private CartVM GetCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json)) return new CartVM();
        return JsonSerializer.Deserialize<CartVM>(json) ?? new CartVM();
    }
}
