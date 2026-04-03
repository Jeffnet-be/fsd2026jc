namespace ChampionsLeague.Domain.Entities;

/// <summary>
/// A single Champions League fixture between two clubs played at the home club's stadium.
/// Business rules are enforced via computed properties — controllers just read these flags.
/// </summary>
public class Match
{
    public int Id { get; set; }

    public int HomeClubId { get; set; }
    public Club HomeClub { get; set; } = null!;

    public int AwayClubId { get; set; }
    public Club AwayClub { get; set; } = null!;

    /// <summary>Date and time kick-off (UTC).</summary>
    public DateTime MatchDate { get; set; }

    /// <summary>Phase of the competition, e.g. "Group Stage", "Quarter-Final".</summary>
    public string Phase { get; set; } = string.Empty;

    /// <summary>
    /// Business rule: tickets on sale from 1 month before kick-off UNTIL kick-off.
    /// Past matches are always closed — you cannot buy tickets after the match has started.
    /// </summary>
    public bool IsSaleOpen =>
        DateTime.UtcNow >= MatchDate.AddMonths(-1) &&
        DateTime.UtcNow <  MatchDate;

    /// <summary>
    /// Business rule: free cancellation up to 7 days before kick-off.
    /// Past matches cannot be cancelled.
    /// </summary>
    public bool IsCancellable =>
        DateTime.UtcNow <  MatchDate &&
        DateTime.UtcNow <= MatchDate.AddDays(-7);

    /// <summary>All tickets sold for this match across all sectors.</summary>
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    /// <summary>All order lines that reference this match.</summary>
    public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
}
