namespace FullStackDevelopment_Ticketverkoop.Domain.Entities;

/// <summary>
/// Represents the home stadium of a club.
/// Every stadium has at least 12,000 seats divided across 8 section types.
/// </summary>
public class Stadium
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int ClubId { get; set; }

    // Navigation properties
    public Club? Club { get; set; }
    public ICollection<SectionType> SectionTypes { get; set; } = new List<SectionType>();
}