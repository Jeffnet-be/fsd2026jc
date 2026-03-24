using ChampionsLeague.Domain.Entities;

namespace ChampionsLeague.Domain.Interfaces;

/// <summary>Club and stadium data access with sector detail loading.</summary>
public interface IClubRepository : IRepository<Club>
{
    /// <summary>Returns all clubs with Stadium and Sector navigation loaded.</summary>
    Task<IEnumerable<Club>> GetAllWithStadiumsAsync();

    /// <summary>Returns one club with its stadium and all sector definitions.</summary>
    Task<Club?> GetWithStadiumAndSectorsAsync(int clubId);
}
