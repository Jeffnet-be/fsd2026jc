using ChampionsLeague.Domain.Entities;

namespace ChampionsLeague.Domain.Interfaces;

/// <summary>
/// Ticket-specific data access: seat availability, voucher lookup, and user purchase history.
/// </summary>
public interface ITicketRepository : IRepository<Ticket>
{
    /// <summary>
    /// Returns all active tickets for a user, eagerly loading Match and Sector
    /// so the history view can display all relevant information in one query.
    /// </summary>
    Task<IEnumerable<Ticket>> GetUserTicketsAsync(string userId);

    /// <summary>
    /// Finds a ticket by its unique voucher GUID.
    /// Used for PDF generation and voucher validation at the gate.
    /// </summary>
    Task<Ticket?> GetByVoucherAsync(Guid voucherId);

    /// <summary>
    /// Returns all occupied seat numbers in a sector for a match.
    /// Called before seat assignment to prevent double-booking.
    /// </summary>
    Task<IEnumerable<int>> GetReservedSeatsAsync(int matchId, int sectorId);
}
