using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Services;

namespace ChampionsLeague.Services;

/// <summary>
/// Implementatie van <see cref="IUserTicketService"/>.
/// Combineert ITicketRepository en IEmailService om
/// AccountController vrij te houden van infrastructuur-details.
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
    public Task<IEnumerable<Ticket>> GetHistoryAsync(string userId)
        => _tickets.GetUserTicketHistoryAsync(userId);

    /// <inheritdoc/>
    public Task<IEnumerable<Ticket>> GetActiveTicketsAsync(string userId)
        => _tickets.GetUserTicketsAsync(userId);

    /// <summary>
    /// Zoekt het ticket op, controleert eigenaarschap en status,
    /// en stuurt de voucher-e-mail opnieuw.
    /// Al deze logica zat vroeger in AccountController — dat hoort hier.
    /// </summary>
    public async Task<(bool Success, string? Error)> ResendVoucherAsync(
        int ticketId, string userId, string userEmail, string userFirstName, string language)
    {
        var tickets = await _tickets.GetUserTicketsAsync(userId);
        var ticket  = tickets.FirstOrDefault(t => t.Id == ticketId);

        if (ticket is null)
            return (false, "Ticket not found.");

        if (ticket.Status == TicketStatus.Cancelled)
            return (false, "Cannot resend voucher for a cancelled ticket.");

        // Taalspecifieke e-mail-inhoud
        var (subject, intro, sectorLabel, seatLabel, voucherLabel, footer) = language switch
        {
            "fr" => ("Votre bon — CL Tickets",
                     $"Voici votre bon pour {ticket.Match?.HomeClub?.Name} vs {ticket.Match?.AwayClub?.Name}:",
                     "Secteur", "Siège", "Bon", "Présentez ce bon à l'entrée du stade."),
            "en" => ("Your voucher — CL Tickets",
                     $"Here is your voucher for {ticket.Match?.HomeClub?.Name} vs {ticket.Match?.AwayClub?.Name}:",
                     "Sector", "Seat", "Voucher", "Present this voucher at the stadium entrance."),
            _ =>   ($"Uw voucher — CL Tickets",
                    $"Hieronder vindt u uw voucher voor {ticket.Match?.HomeClub?.Name} vs {ticket.Match?.AwayClub?.Name}:",
                    "Vak", "Zitplaats", "Voucher", "Toon uw voucher aan de ingang van het stadion.")
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
}
