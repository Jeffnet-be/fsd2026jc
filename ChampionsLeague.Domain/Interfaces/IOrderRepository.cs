using ChampionsLeague.Domain.Entities;

namespace ChampionsLeague.Domain.Interfaces;

/// <summary>
/// Order data access — includes full eager-load queries for checkout display
/// and the business-rule check for duplicate match dates.
/// </summary>
public interface IOrderRepository : IRepository<Order>
{
    /// <summary>Returns an order with all lines, matches, sectors, and tickets loaded.</summary>
    Task<Order?> GetWithLinesAsync(int orderId);

    /// <summary>Returns all paid orders for a user, ordered by date descending.</summary>
    Task<IEnumerable<Order>> GetUserOrdersAsync(string userId);

    /// <summary>
    /// Business-rule check: returns true if the user already has a confirmed ticket
    /// for any match on the same calendar day as matchDate.
    /// Enforces "no two matches on the same day" from the project spec.
    /// </summary>
    Task<bool> UserHasMatchOnDayAsync(string userId, DateTime matchDate);
}
