using FullStackDevelopment_Ticketverkoop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FullStackDevelopment_Ticketverkoop.Data.Repositories;

/// <summary>
/// Concrete EF Core implementation of IMatchRepository.
/// Uses LINQ with eager loading (Include) to fetch related entities
/// in a single database query.
/// </summary>
public class MatchRepository : IMatchRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Constructor injection — EF Core's AppDbContext is provided by the DI container.
    /// </summary>
    public MatchRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns all matches with their clubs and stadiums loaded.</summary>
    public async Task<IEnumerable<Match>> GetAllAsync()
    {
        return await _context.Matches
            .Include(m => m.HomeClub).ThenInclude(c => c!.Stadium)
            .Include(m => m.AwayClub)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();
    }

    /// <summary>Returns all matches where the given club plays home or away.</summary>
    public async Task<IEnumerable<Match>> GetByClubAsync(int clubId)
    {
        return await _context.Matches
            .Include(m => m.HomeClub).ThenInclude(c => c!.Stadium)
            .Include(m => m.AwayClub)
            .Where(m => m.HomeClubId == clubId || m.AwayClubId == clubId)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();
    }

    /// <summary>Returns a single match with full section and ticket details.</summary>
    public async Task<Match?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Matches
            .Include(m => m.HomeClub).ThenInclude(c => c!.Stadium)
                .ThenInclude(s => s!.SectionTypes)
            .Include(m => m.AwayClub)
            .Include(m => m.Tickets)
            .FirstOrDefaultAsync(m => m.Id == id);
    }
}