using ChampionsLeague.Domain.Entities;

namespace ChampionsLeague.Domain.Interfaces;

/// <summary>Season ticket (abonnement) queries.</summary>
public interface ISeasonTicketRepository : IRepository<SeasonTicket>
{
    /// <summary>Returns all active season tickets for a user.</summary>
    Task<IEnumerable<SeasonTicket>> GetUserSeasonTicketsAsync(string userId);

    /// <summary>
    /// Returns seat numbers in a sector that are permanently reserved by season tickets.
    /// These seats must be excluded from single-game ticket sales to prevent overbooking.
    /// </summary>
    Task<IEnumerable<int>> GetSeasonReservedSeatsAsync(int sectorId);

    /// <summary>Returns ALL season tickets for a user including cancelled ones.</summary>
    Task<IEnumerable<SeasonTicket>> GetAllUserSeasonTicketsAsync(string userId);
}
