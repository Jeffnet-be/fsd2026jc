using FullStackDevelopment_Ticketverkoop.Domain.Entities;

namespace FullStackDevelopment_Ticketverkoop.Data.Repositories;

/// <summary>
/// Contract (interface) for all match-related data access.
/// Programming against this interface — not against MatchRepository directly —
/// is the Dependency Inversion Principle in action.
/// </summary>
public interface IMatchRepository
{
    Task<IEnumerable<Match>> GetAllAsync();
    Task<IEnumerable<Match>> GetByClubAsync(int clubId);
    Task<Match?> GetByIdWithDetailsAsync(int id);
}