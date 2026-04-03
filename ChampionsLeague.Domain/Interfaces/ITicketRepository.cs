using ChampionsLeague.Domain.Entities;

namespace ChampionsLeague.Domain.Interfaces;

public interface ITicketRepository : IRepository<Ticket>
{
    /// <summary>Returns all active (non-cancelled) tickets for a user.</summary>
    Task<IEnumerable<Ticket>> GetUserTicketsAsync(string userId);

    /// <summary>Returns all active (non-cancelled) AND cancelled tickets for a user (full history).</summary>
    Task<IEnumerable<Ticket>> GetUserTicketHistoryAsync(string userId);

    /// <summary>Returns a ticket by its voucher GUID.</summary>
    Task<Ticket?> GetByVoucherAsync(Guid voucherId);

    /// <summary>
    /// Returns seat numbers already reserved in a sector for a match (excluding cancelled).
    /// Used for capacity checks and overbooking prevention.
    /// </summary>
    Task<IEnumerable<int>> GetReservedSeatsAsync(int matchId, int sectorId);

    /// <summary>
    /// Returns how many active (non-cancelled) tickets a user has for a specific match.
    /// Used to enforce the max-4-tickets-per-person-per-match rule across multiple orders.
    /// </summary>
    Task<int> GetUserTicketCountForMatchAsync(string userId, int matchId);
}
