namespace ChampionsLeague.Domain.Entities;

/// <summary>
/// A single Champions League fixture between two clubs played at the home club's stadium.
/// Business rule: tickets may only be purchased from one month before MatchDate.
/// Computed properties enforce the rules without duplicating logic in controllers.
/// </summary>
public class Match
{
    public int Id { get; set; }

    public int HomeClubId { get; set; }
    public Club HomeClub { get; set; } = null!;

    public int AwayClubId { get; set; }
    public Club AwayClub { get; set; } = null!;

    /// <summary>Date and time kick-off (UTC stored, displayed in local TZ in views).</summary>
    public DateTime MatchDate { get; set; }

    /// <summary>Phase of the competition, e.g. "Group Stage", "Quarter-Final".</summary>
    public string Phase { get; set; } = string.Empty;

    /// <summary>
    /// Derived: tickets become available exactly one calendar month before MatchDate.
    /// Business rule enforced here — controllers just read this flag.
    /// </summary>
    public bool IsSaleOpen => DateTime.UtcNow >= MatchDate.AddMonths(-1);

    /// <summary>
    /// Derived: free cancellation is possible up to one week before MatchDate.
    /// </summary>
    public bool IsCancellable => DateTime.UtcNow <= MatchDate.AddDays(-7);

    /// <summary>All tickets sold for this match across all sectors.</summary>
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    /// <summary>All order lines that reference this match.</summary>
    public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
}
