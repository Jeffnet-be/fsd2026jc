namespace ChampionsLeague.Domain.Entities;

/// <summary>
/// Represents one of the six participating Champions League clubs.
/// Each club owns exactly one Stadium in this domain model.
/// </summary>
public class Club
{
    public int Id { get; set; }

    /// <summary>Official club name, e.g. "Real Madrid".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Country the club is based in.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Path to the club badge</summary>
    public string BadgeUrl { get; set; } = string.Empty;

    /// <summary>Primary hex colour used for UI theming.</summary>
    public string PrimaryColor { get; set; } = "#000000";

    /// <summary>The stadium belonging to this club (one-to-one).</summary>
    public Stadium? Stadium { get; set; }

    /// <summary>All home matches scheduled for this club.</summary>
    public ICollection<Match> HomeMatches { get; set; } = new List<Match>();

    /// <summary>All away matches scheduled for this club.</summary>
    public ICollection<Match> AwayMatches { get; set; } = new List<Match>();
}
