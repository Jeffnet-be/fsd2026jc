namespace FullStackDevelopment_Ticketverkoop.Domain.Entities;

/// <summary>
/// Represents a single Champions League match between a home club and an away club.
/// Tickets can only be purchased starting 1 month before the match date.
/// </summary>
public class Match
{
    public int Id { get; set; }
    public DateTime MatchDate { get; set; }
    public string Phase { get; set; } = string.Empty; // e.g. "Group Stage", "Quarter Final"

    public int HomeClubId { get; set; }
    public Club? HomeClub { get; set; }

    public int AwayClubId { get; set; }
    public Club? AwayClub { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}