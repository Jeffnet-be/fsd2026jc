using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Infrastructure.Services;
using ChampionsLeague.Services;
using ChampionsLeague.Services.DTOs;
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
/// LAGENREGEL: Injecteert enkel service-interfaces.
/// De Web-ViewModel SeasonCartItemVM (sessie) wordt hier omgezet naar
/// SeasonCartItemDto (Services.DTOs) voordat het naar de service gaat.
/// Zo weet de service-laag niets van Web-ViewModels.
/// </summary>
[Authorize]
public class CheckoutController : Controller
{
    private const string CartSessionKey = "CART";

    // Zelfde opties als CartController: camelCase JSON lezen/schrijven
    // zodat MatchId en SectorId correct gedeserialiseerd worden.
    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy        = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    private readonly ITicketService       _ticketService;
    private readonly ISeasonTicketService _seasonTicketService;
    private readonly IMatchService        _matchService;
    private readonly IEmailService        _email;
    private readonly TranslationService   _tr;

    public CheckoutController(
        ITicketService       ticketService,
        ISeasonTicketService seasonTicketService,
        IMatchService        matchService,
        IEmailService        email,
        TranslationService   tr)
    {
        _ticketService       = ticketService;
        _seasonTicketService = seasonTicketService;
        _matchService        = matchService;
        _email               = email;
        _tr                  = tr;
    }

    public IActionResult Review()
    {
        var cart = GetCart();
        if (!cart.Items.Any() && !cart.SeasonItems.Any())
            return RedirectToAction("Index", "Cart");
        return View(cart);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm()
    {
        var cart   = GetCart();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var errors = new List<string>();

        var purchasedTickets       = new List<(Ticket ticket, Match match)>();
        var purchasedSeasonTickets = new List<(SeasonTicketDto dto, string sectorName, string stadiumName)>();

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
                // Vertaal de foutcode naar de huidige taal
                var errMsg = result.ErrorCode switch
                {
                    PurchaseErrorCode.SaleNotOpen      => string.Format(_tr.T("purchase_err_sale_not_open"), result.SaleOpensOn.ToString("dd/MM/yyyy")),
                    PurchaseErrorCode.MaxTicketsExceeded => string.Format(_tr.T("purchase_err_max_tickets"), result.AlreadyOwned, 4 - result.AlreadyOwned),
                    PurchaseErrorCode.MinQuantity       => _tr.T("purchase_err_min_quantity"),
                    PurchaseErrorCode.SameDayMatch      => _tr.T("purchase_err_same_day"),
                    PurchaseErrorCode.SectorNotFound    => _tr.T("purchase_err_sector_not_found"),
                    PurchaseErrorCode.NotEnoughSeats    => string.Format(_tr.T("purchase_err_not_enough_seats"), result.SeatsLeft),
                    PurchaseErrorCode.MatchNotFound     => _tr.T("purchase_err_match_not_found"),
                    _                                   => result.ErrorMessage ?? "Onbekende fout."
                };
                errors.Add($"{item.MatchDescription} — {errMsg}");
            }
            else if (result.Tickets is not null)
            {
                var matches = await _matchService.GetAllWithClubsAsync();
                var match   = matches.FirstOrDefault(m => m.Id == item.MatchId);
                if (match is not null)
                    foreach (var t in result.Tickets)
                        purchasedTickets.Add((t, match));
            }
        }

        // ── Seizoensabonnementen ───────────────────────────────────────
        foreach (var item in cart.SeasonItems)
        {
            // Web-ViewModel → Service-DTO (conversie in de Web-laag, correct)
            var dto = new SeasonCartItemDto
            {
                SectorId    = item.SectorId,
                SectorName  = item.SectorName,
                StadiumName = item.StadiumName,
                ClubName    = item.ClubName,
                TotalPrice  = item.TotalPrice
            };

            var (success, error, created) = await _seasonTicketService.FinalizeAsync(userId, dto);

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

        HttpContext.Session.Remove(CartSessionKey);

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
        var firstName = User.FindFirstValue(ClaimTypes.GivenName) ?? "";

        if (purchasedTickets.Any() || purchasedSeasonTickets.Any())
            await SendConfirmationEmailAsync(userEmail, firstName, purchasedTickets, purchasedSeasonTickets);

        TempData["Success"] = _tr.T("checkout_confirmed_msg");
        return RedirectToAction(nameof(Confirmation));
    }

    public IActionResult Confirmation() => View();

    // ── E-mail helper ─────────────────────────────────────────────────────

    private async Task SendConfirmationEmailAsync(
        string userEmail, string firstName,
        List<(Ticket ticket, Match match)> tickets,
        List<(SeasonTicketDto dto, string sectorName, string stadiumName)> seasonTickets)
    {
        var lang = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
        if (lang is not ("nl" or "fr" or "en")) lang = "nl";

        var (subject, intro, matchLabel, seatLabel, voucherLabel, seasonSection, footer) = lang switch
        {
            "fr" => ("Confirmation de votre commande — CL Tickets",
                     $"Merci, {firstName}!", "Match", "Siège", "Bon",
                     "Abonnements saison", "Présentez ce bon à l'entrée du stade."),
            "en" => ("Your order confirmation — CL Tickets",
                     $"Thank you, {firstName}!", "Match", "Seat", "Voucher",
                     "Season tickets", "Present your voucher at the stadium entrance."),
            _ =>   ("Bevestiging van uw bestelling — CL Tickets",
                    $"Bedankt, {firstName}!", "Wedstrijd", "Zitplaats", "Voucher",
                    "Seizoensabonnementen", "Toon uw voucher aan de ingang van het stadion.")
        };

        var ticketRows = string.Join("", tickets.GroupBy(p => p.match.Id).Select(g =>
        {
            var m    = g.First().match;
            var desc = $"{m.HomeClub?.Name} vs {m.AwayClub?.Name}";
            var date = m.MatchDate.ToString("dd MMMM yyyy HH:mm") + " UTC";
            var rows = string.Join("", g.Select(p =>
                $"<tr style='background:#f8f9ff'><td style='padding:6px 16px 6px 24px'>{seatLabel} {p.ticket.SeatNumber}</td>" +
                $"<td style='font-family:monospace;font-size:12px;color:#001489'>{p.ticket.VoucherId:D}</td></tr>"));
            return $"<tr style='background:#001489'><td colspan='2' style='padding:10px 16px;color:#FFD700;font-weight:bold'>{desc} — {date}</td></tr>{rows}";
        }));

        var seasonRows = seasonTickets.Any()
            ? $"<tr style='background:#C8A600'><td colspan='2' style='padding:10px 16px;color:#001489;font-weight:bold'>{seasonSection}</td></tr>"
              + string.Join("", seasonTickets.Select(s =>
                  $"<tr style='background:#f8f9ff'><td style='padding:8px 16px'>{s.stadiumName} — {s.sectorName}</td>" +
                  $"<td style='font-weight:bold;color:#001489'>{seatLabel} {s.dto.SeatNumber}</td></tr>"))
            : "";

        await _email.SendAsync(
            to      : userEmail,
            subject : subject,
            htmlBody: $@"<p>{intro}</p>
<table style='border-collapse:collapse;font-family:Arial,sans-serif;width:100%;max-width:580px'>
  <tr style='background:#001489'><th style='padding:10px 16px;color:#FFD700;text-align:left'>{matchLabel}</th>
  <th style='padding:10px 16px;color:#FFD700;text-align:left'>{voucherLabel}</th></tr>
  {ticketRows}{seasonRows}
</table>
<p style='margin-top:16px'>{footer}</p><p>CL Tickets Portal</p>"
        );
    }

    private CartVM GetCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json)) return new CartVM();
        return JsonSerializer.Deserialize<CartVM>(json, _jsonOptions) ?? new CartVM();
    }
}
