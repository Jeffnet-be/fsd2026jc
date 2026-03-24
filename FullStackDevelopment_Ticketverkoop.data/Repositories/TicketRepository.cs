using FullStackDevelopment_Ticketverkoop.Domain.Entities;
using FullStackDevelopment_Ticketverkoop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FullStackDevelopment_Ticketverkoop.Data.Repositories;

/// <summary>
/// EF Core implementation for all ticket data operations.
/// All write operations call SaveChangesAsync to persist to SQL Server.
/// </summary>
public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _context;

    public TicketRepository(AppDbContext context) => _context = context;

    /// <summary>Returns all available (unsold) tickets for a given match and section.</summary>
    public async Task<IEnumerable<Ticket>> GetAvailableByMatchAndSectionAsync(
        int matchId, int sectionTypeId)
    {
        return await _context.Tickets
            .Where(t => t.MatchId == matchId
                     && t.SectionTypeId == sectionTypeId
                     && t.Status == TicketStatus.Available)
            .ToListAsync();
    }

    public async Task<Ticket?> GetByIdAsync(int id) =>
        await _context.Tickets
            .Include(t => t.Match).ThenInclude(m => m!.HomeClub)
            .Include(t => t.SectionType)
            .FirstOrDefaultAsync(t => t.Id == id);

    /// <summary>
    /// Counts how many tickets this user has already bought for this specific match.
    /// Business rule: max 4 per person per match.
    /// </summary>
    public async Task<int> CountSoldByUserForMatchAsync(string userId, int matchId)
    {
        return await _context.Tickets
            .CountAsync(t => t.BuyerUserId == userId
                          && t.MatchId == matchId
                          && t.Status == TicketStatus.Sold);
    }

    /// <summary>
    /// Checks whether the user already has a ticket for any match on the same day.
    /// Business rule: a person cannot attend two matches on the same day.
    /// </summary>
    public async Task<bool> HasUserBoughtOnSameDayAsync(
        string userId, DateTime matchDate, int excludeMatchId)
    {
        return await _context.Tickets
            .Include(t => t.Match)
            .AnyAsync(t => t.BuyerUserId == userId
                        && t.Status == TicketStatus.Sold
                        && t.MatchId != excludeMatchId
                        && t.Match!.MatchDate.Date == matchDate.Date);
    }

    public async Task UpdateAsync(Ticket ticket)
    {
        _context.Tickets.Update(ticket);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<Ticket> tickets)
    {
        _context.Tickets.UpdateRange(tickets);
        await _context.SaveChangesAsync();
    }

    /// <summary>Returns all tickets purchased by a specific user (order history).</summary>
    public async Task<IEnumerable<Ticket>> GetByUserIdAsync(string userId)
    {
        return await _context.Tickets
            .Include(t => t.Match).ThenInclude(m => m!.HomeClub)
            .Include(t => t.Match).ThenInclude(m => m!.AwayClub)
            .Include(t => t.SectionType)
            .Where(t => t.BuyerUserId == userId && t.Status == TicketStatus.Sold)
            .OrderByDescending(t => t.Match!.MatchDate)
            .ToListAsync();
    }
}