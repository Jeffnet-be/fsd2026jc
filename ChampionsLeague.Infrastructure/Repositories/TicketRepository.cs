using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChampionsLeague.Infrastructure.Repositories;

/// <summary>
/// Concrete Ticket repository with user-history and seat-availability queries.
/// All queries use eager loading (Include/ThenInclude) to avoid N+1 problems.
/// </summary>
public class TicketRepository : BaseRepository<Ticket>, ITicketRepository
{
    public TicketRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<IEnumerable<Ticket>> GetUserTicketsAsync(string userId)
        => await _set
            .Where(t => t.OrderLine.Order.UserId == userId
                     && t.Status != TicketStatus.Cancelled)
            .Include(t => t.Match).ThenInclude(m => m.HomeClub)
            .Include(t => t.Match).ThenInclude(m => m.AwayClub)
            .Include(t => t.Sector)
            .Include(t => t.OrderLine).ThenInclude(ol => ol.Order)
            .OrderByDescending(t => t.Match.MatchDate)
            .AsNoTracking()
            .ToListAsync();

    /// <inheritdoc/>
    public async Task<Ticket?> GetByVoucherAsync(Guid voucherId)
        => await _set
            .Include(t => t.Match).ThenInclude(m => m.HomeClub)
            .Include(t => t.Match).ThenInclude(m => m.AwayClub)
            .Include(t => t.Sector)
            .FirstOrDefaultAsync(t => t.VoucherId == voucherId);

    /// <inheritdoc/>
    public async Task<IEnumerable<int>> GetReservedSeatsAsync(int matchId, int sectorId)
        => await _set
            .Where(t => t.MatchId  == matchId
                     && t.SectorId == sectorId
                     && t.Status   != TicketStatus.Cancelled)
            .Select(t => t.SeatNumber)
            .ToListAsync();
}
