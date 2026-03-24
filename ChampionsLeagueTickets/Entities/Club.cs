namespace FullStackDevelopment_Ticketverkoop.Domain.Entities;

/// <summary>
/// Represents one of the six participating Champions League clubs.
/// Maps to the Clubs table in the database.
/// </summary>
public class Club
{
    public int Id { get; set; }

    /// <summary>Full name of the club, e.g. "Real Madrid".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Country where the club is based.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>URL to the club's logo image (stored in wwwroot).</summary>
    public string LogoUrl { get; set; } = string.Empty;

    // Navigation properties
    public Stadium? Stadium { get; set; }
    public ICollection<Match> HomeMatches { get; set; } = new List<Match>();
    public ICollection<Match> AwayMatches { get; set; } = new List<Match>();
}