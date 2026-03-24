using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChampionsLeague.Infrastructure.Repositories;

/// <summary>Concrete Club repository with full stadium + sector eager loading.</summary>
public class ClubRepository : BaseRepository<Club>, IClubRepository
{
    public ClubRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<IEnumerable<Club>> GetAllWithStadiumsAsync()
        => await _set
            .Include(c => c.Stadium).ThenInclude(s => s.Sectors)
            .AsNoTracking()
            .ToListAsync();

    /// <inheritdoc/>
    public async Task<Club?> GetWithStadiumAndSectorsAsync(int clubId)
        => await _set
            .Include(c => c.Stadium).ThenInclude(s => s.Sectors)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clubId);
}
