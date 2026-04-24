using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;

namespace ChampionsLeague.Services;

/// <summary>
/// Implementatie van <see cref="IClubService"/>.
/// Wraps de clubrepository zodat de Web-laag geen directe repository-afhankelijkheden heeft.
/// </summary>
public class ClubService : IClubService
{
    private readonly IClubRepository _clubs;

    public ClubService(IClubRepository clubs)
    {
        _clubs = clubs;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Club>> GetAllWithStadiumsAsync()
        => _clubs.GetAllWithStadiumsAsync();

    /// <inheritdoc/>
    public Task<IEnumerable<Club>> GetAllAsync()
        => _clubs.GetAllAsync();

    /// <inheritdoc/>
    public Task<Club?> GetWithStadiumAndSectorsAsync(int clubId)
        => _clubs.GetWithStadiumAndSectorsAsync(clubId);
}
