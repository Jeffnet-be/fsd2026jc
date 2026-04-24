using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChampionsLeague.Infrastructure.Repositories;

/// <summary>
/// Concrete Club repository with full stadium + sector eager loading.
/// Null-conditional operators suppress CS8602 warnings that appear in
/// CI builds where nullable analysis is stricter than local builds.
/// </summary>
public class ClubRepository : BaseRepository<Club>, IClubRepository
{
    public ClubRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<IEnumerable<Club>> GetAllWithStadiumsAsync()
        => await _set
            .Include(c => c.Stadium!)          // ! = we know Stadium is loaded
                .ThenInclude(s => s.Sectors)
            .AsNoTracking()
            .ToListAsync();

    /// <inheritdoc/>
    public async Task<Club?> GetWithStadiumAndSectorsAsync(int clubId)
        => await _set
            .Include(c => c.Stadium!)
                .ThenInclude(s => s.Sectors)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clubId);

    /// <inheritdoc/>
    public async Task<Sector?> GetSectorByIdAsync(int sectorId)
        => await _context.Set<Sector>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sectorId);
}