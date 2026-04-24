using ChampionsLeague.Domain.Entities;

namespace ChampionsLeague.Services;

/// <summary>
/// Service-laag contract voor gebruiker-specifieke ticket-operaties:
/// historiek, voucher-info, annulatie.
///
/// Waarom een aparte service voor de AccountController?
/// De AccountController had vroeger ITicketRepository, ISeasonTicketRepository én
/// IMatchRepository direct geïnjecteerd. Die controller deed dan zelf LINQ-queries
/// om data samen te stellen. Dat hoort niet in de Web-laag.
/// Door deze service te introduceren bevat AccountController geen repository-calls meer.
/// </summary>
public interface IUserTicketService
{
    /// <summary>
    /// Geeft alle ticketgeschiedenis (inclusief geannuleerde) terug voor een gebruiker,
    /// samen met de match-info, klaar om te mappen naar een ViewModel.
    /// </summary>
    Task<IEnumerable<Ticket>> GetHistoryAsync(string userId);

    /// <summary>Geeft actieve (niet-geannuleerde) tickets terug voor een gebruiker.</summary>
    Task<IEnumerable<Ticket>> GetActiveTicketsAsync(string userId);

    /// <summary>
    /// Stuurt de voucher-e-mail opnieuw voor één specifiek ticket.
    /// Bevat de controle of het ticket toebehoort aan de gebruiker en niet geannuleerd is.
    /// </summary>
    Task<(bool Success, string? Error)> ResendVoucherAsync(
        int ticketId, string userId, string userEmail, string userFirstName, string language);
}
