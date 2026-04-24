using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Services;
using ChampionsLeague.Services.DTOs;

namespace ChampionsLeague.Services;

/// <summary>
/// Service-laag contract voor gebruiker-ticket-operaties.
/// Retourneert <see cref="TicketHistoryDto"/> — geen Web.ViewModels.
/// </summary>
public interface IUserTicketService
{
    /// <summary>Volledige ticketgeschiedenis (incl. geannuleerde) als DTOs.</summary>
    Task<IEnumerable<TicketHistoryDto>> GetHistoryAsync(string userId);

    /// <summary>Actieve (niet-geannuleerde) tickets als DTOs.</summary>
    Task<IEnumerable<TicketHistoryDto>> GetActiveTicketsAsync(string userId);

    /// <summary>
    /// Geeft ruwe Ticket-entiteiten terug (met navigatie) voor gebruik in
    /// CheckoutController e-mail en API-mapping.
    /// </summary>
    Task<IEnumerable<Ticket>> GetActiveTicketEntitiesAsync(string userId);

    /// <summary>
    /// Stuurt een voucher-e-mail opnieuw voor één ticket.
    /// Controleert eigenaarschap en status voordat de e-mail verstuurd wordt.
    /// </summary>
    Task<(bool Success, string? Error)> ResendVoucherAsync(
        int ticketId, string userId, string userEmail, string userFirstName, string language);
}

/// <summary>
/// Implementatie van <see cref="IUserTicketService"/>.
/// Mapt Ticket-entiteiten naar TicketHistoryDto — geen Web-afhankelijkheden.
/// </summary>
public class UserTicketService : IUserTicketService
{
    private readonly ITicketRepository _tickets;
    private readonly IEmailService     _email;

    public UserTicketService(ITicketRepository tickets, IEmailService email)
    {
        _tickets = tickets;
        _email   = email;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TicketHistoryDto>> GetHistoryAsync(string userId)
    {
        var entities = await _tickets.GetUserTicketHistoryAsync(userId);
        return entities.Select(ToDto);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TicketHistoryDto>> GetActiveTicketsAsync(string userId)
    {
        var entities = await _tickets.GetUserTicketsAsync(userId);
        return entities.Select(ToDto);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Ticket>> GetActiveTicketEntitiesAsync(string userId)
        => _tickets.GetUserTicketsAsync(userId);

    /// <summary>
    /// Stuurt voucher-e-mail opnieuw. Controleert eerst:
    /// 1. Ticket bestaat en behoort toe aan userId.
    /// 2. Ticket is niet geannuleerd.
    /// </summary>
    public async Task<(bool Success, string? Error)> ResendVoucherAsync(
        int ticketId, string userId, string userEmail, string userFirstName, string language)
    {
        var tickets = await _tickets.GetUserTicketsAsync(userId);
        var ticket  = tickets.FirstOrDefault(t => t.Id == ticketId);

        if (ticket is null)
            return (false, "Ticket niet gevonden.");

        if (ticket.Status == TicketStatus.Cancelled)
            return (false, "Voucher kan niet opnieuw verstuurd worden voor een geannuleerd ticket.");

        var matchDesc = ticket.Match is not null
            ? $"{ticket.Match.HomeClub?.Name} vs {ticket.Match.AwayClub?.Name}"
            : "Onbekende wedstrijd";

        var (subject, intro, sectorLabel, seatLabel, voucherLabel, footer) = language switch
        {
            "fr" => ("Votre bon — CL Tickets",
                     $"Voici votre bon pour {matchDesc}:",
                     "Secteur", "Siège", "Bon",
                     "Présentez ce bon à l'entrée du stade."),
            "en" => ("Your voucher — CL Tickets",
                     $"Here is your voucher for {matchDesc}:",
                     "Sector", "Seat", "Voucher",
                     "Present this voucher at the stadium entrance."),
            _ =>   ("Uw voucher — CL Tickets",
                    $"Hieronder vindt u uw voucher voor {matchDesc}:",
                    "Vak", "Zitplaats", "Voucher",
                    "Toon uw voucher aan de ingang van het stadion.")
        };

        await _email.SendAsync(
            to      : userEmail,
            subject : subject,
            htmlBody: $@"
<p>Hallo {userFirstName},</p>
<p>{intro}</p>
<table style='border-collapse:collapse;font-family:Arial,sans-serif'>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>{sectorLabel}:</td>
      <td style='padding:6px 0;font-weight:bold'>{ticket.Sector?.Name ?? ""}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>{seatLabel}:</td>
      <td style='padding:6px 0;font-weight:bold'>{ticket.SeatNumber}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>{voucherLabel}:</td>
      <td style='padding:6px 0;font-family:monospace;font-size:14px;font-weight:bold;color:#001489'>{ticket.VoucherId:D}</td></tr>
</table>
<p>{footer}</p>
<p>CL Tickets Portal</p>"
        );

        return (true, null);
    }

    // ── Mapping helper ───────────────────────────────────────────────────

    private static TicketHistoryDto ToDto(Ticket t) => new()
    {
        Id               = t.Id,
        MatchDescription = t.Match is not null
            ? $"{t.Match.HomeClub?.Name} vs {t.Match.AwayClub?.Name}"
            : "Onbekende wedstrijd",
        MatchDate        = t.Match?.MatchDate ?? DateTime.MinValue,
        SectorName       = t.Sector?.Name ?? string.Empty,
        SeatNumber       = t.SeatNumber,
        PricePaid        = t.PricePaid,
        VoucherId        = t.VoucherId,
        Status           = t.Status.ToString(),
        IsCancellable    = t.Match?.IsCancellable == true && t.Status != TicketStatus.Cancelled
    };
}
