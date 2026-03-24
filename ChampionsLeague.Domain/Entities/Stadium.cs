namespace ChampionsLeague.Domain.Entities;

/// <summary>
/// Represents a real football stadium owned by a Club.
/// Contains the eight sector (vaktype) definitions with individual capacities.
/// </summary>
public class Stadium
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>City where the stadium is located — used for hotel search.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>FK back to the owning Club.</summary>
    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;

    /// <summary>
    /// The eight predefined sectors (vaktypes) inside this stadium.
    /// Each sector has its own capacity and base ticket price.
    /// </summary>
    public ICollection<Sector> Sectors { get; set; } = new List<Sector>();
}
