using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChampionsLeague.Infrastructure.Repositories;

/// <summary>
/// Concrete Order repository. Includes the business-rule query that prevents
/// a user from buying tickets for two matches on the same calendar day.
/// </summary>
public class OrderRepository : BaseRepository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Order?> GetWithLinesAsync(int orderId)
        => await _set
            .Where(o => o.Id == orderId)
            .Include(o => o.OrderLines).ThenInclude(ol => ol.Match)
                .ThenInclude(m => m.HomeClub)
            .Include(o => o.OrderLines).ThenInclude(ol => ol.Match)
                .ThenInclude(m => m.AwayClub)
            .Include(o => o.OrderLines).ThenInclude(ol => ol.Sector)
            .Include(o => o.OrderLines).ThenInclude(ol => ol.Tickets)
            .FirstOrDefaultAsync();

    /// <inheritdoc/>
    public async Task<IEnumerable<Order>> GetUserOrdersAsync(string userId)
        => await _set
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Paid)
            .Include(o => o.OrderLines).ThenInclude(ol => ol.Match)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

    /// <inheritdoc/>
    public async Task<bool> UserHasMatchOnDayAsync(string userId, DateTime matchDate)
    {
        var day = matchDate.Date;
        return await _context.OrderLines
            .Where(ol => ol.Order.UserId    == userId
                      && ol.Order.Status    == OrderStatus.Paid
                      && ol.Match.MatchDate.Date == day)
            .AnyAsync();
    }
}
