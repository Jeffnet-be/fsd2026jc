using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChampionsLeague.Infrastructure.Repositories;

/// <summary>Concrete SeasonTicket (abonnement) repository.</summary>
public class SeasonTicketRepository : BaseRepository<SeasonTicket>, ISeasonTicketRepository
{
    public SeasonTicketRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<IEnumerable<SeasonTicket>> GetUserSeasonTicketsAsync(string userId)
        => await _set
            .Where(st => st.UserId == userId && st.IsActive)
            .Include(st => st.Sector).ThenInclude(s => s.Stadium).ThenInclude(s => s.Club)
            .AsNoTracking()
            .ToListAsync();

    /// <inheritdoc/>
    public async Task<IEnumerable<int>> GetSeasonReservedSeatsAsync(int sectorId)
        => await _set
            .Where(st => st.SectorId == sectorId && st.IsActive)
            .Select(st => st.SeatNumber)
            .ToListAsync();
}
