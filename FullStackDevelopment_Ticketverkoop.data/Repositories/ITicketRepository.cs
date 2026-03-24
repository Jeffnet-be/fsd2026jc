using FullStackDevelopment_Ticketverkoop.Domain.Entities;
using FullStackDevelopment_Ticketverkoop.Domain.Enums;

namespace FullStackDevelopment_Ticketverkoop.Data.Repositories;

/// <summary>
/// Contract for ticket-related data access operations.
/// </summary>
public interface ITicketRepository
{
    Task<IEnumerable<Ticket>> GetAvailableByMatchAndSectionAsync(int matchId, int sectionTypeId);
    Task<Ticket?> GetByIdAsync(int id);
    Task<int> CountSoldByUserForMatchAsync(string userId, int matchId);
    Task<bool> HasUserBoughtOnSameDayAsync(string userId, DateTime matchDate, int excludeMatchId);
    Task UpdateAsync(Ticket ticket);
    Task UpdateRangeAsync(IEnumerable<Ticket> tickets);
    Task<IEnumerable<Ticket>> GetByUserIdAsync(string userId);
}