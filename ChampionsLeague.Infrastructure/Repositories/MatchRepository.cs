using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChampionsLeague.Infrastructure.Repositories;

/// <summary>
/// Concrete Match repository. All LINQ queries demonstrate the strongly-typed
/// IQueryable approach.
/// AsNoTracking() is used on read-only queries for better performance.
/// </summary>
public class MatchRepository : BaseRepository<Match>, IMatchRepository
{
    public MatchRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<IEnumerable<Match>> GetAllWithClubsAsync()
        => await _set
            .Include(m => m.HomeClub).ThenInclude(c => c.Stadium)
            .Include(m => m.AwayClub)
            .OrderBy(m => m.MatchDate)
            .AsNoTracking()
            .ToListAsync();

    /// <inheritdoc/>
    public async Task<IEnumerable<Match>> GetByClubAsync(int clubId)
        => await _set
            .Where(m => m.HomeClubId == clubId || m.AwayClubId == clubId)
            .Include(m => m.HomeClub).ThenInclude(c => c.Stadium)
            .Include(m => m.AwayClub)
            .OrderBy(m => m.MatchDate)
            .AsNoTracking()
            .ToListAsync();

    /// <inheritdoc/>
    public async Task<int> GetSoldCountAsync(int matchId, int sectorId)
        => await _context.Tickets
            .Where(t => t.MatchId  == matchId
                     && t.SectorId == sectorId
                     && t.Status   != TicketStatus.Cancelled)
            .CountAsync();
}
