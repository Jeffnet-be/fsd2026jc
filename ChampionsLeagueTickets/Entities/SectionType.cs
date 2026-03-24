namespace FullStackDevelopment_Ticketverkoop.Domain.Entities;

/// <summary>
/// Represents one of the 8 section types in a stadium
/// (e.g. lower ring behind goal home, upper ring sideline east, etc.).
/// Stores total capacity and the price per seat for this section.
/// </summary>
public class SectionType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Total number of seats in this section.</summary>
    public int Capacity { get; set; }

    /// <summary>Ticket price for this section in EUR.</summary>
    public decimal Price { get; set; }

    public int StadiumId { get; set; }
    public Stadium? Stadium { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}