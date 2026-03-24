using ChampionsLeague.Domain.Entities;

namespace ChampionsLeague.Domain.Interfaces;

/// <summary>
/// Match-specific queries that extend the generic CRUD contract.
/// Keeping these separate from IRepository keeps the generic interface clean
/// and expresses intent: these are queries meaningful only for Match entities.
/// </summary>
public interface IMatchRepository : IRepository<Match>
{
    /// <summary>
    /// Returns all matches with Club and Stadium navigation data eagerly loaded.
    /// Used by the match calendar page that displays club names and stadium info.
    /// </summary>
    Task<IEnumerable<Match>> GetAllWithClubsAsync();

    /// <summary>Returns matches where the given club is either home or away.</summary>
    Task<IEnumerable<Match>> GetByClubAsync(int clubId);

    /// <summary>
    /// Returns the number of non-cancelled tickets already sold
    /// in a given sector for a given match — used to compute availability.
    /// </summary>
    Task<int> GetSoldCountAsync(int matchId, int sectorId);
}
