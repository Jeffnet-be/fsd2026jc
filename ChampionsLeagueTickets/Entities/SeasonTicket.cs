namespace FullStackDevelopment_Ticketverkoop.Domain.Entities;

/// <summary>
/// A season ticket (abonnement) that reserves a specific seat for all home matches
/// of one club. Must be purchased before the competition starts.
/// A seat with a season ticket can no longer be sold as a single ticket.
/// </summary>
public class SeasonTicket
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int SectionTypeId { get; set; }
    public SectionType? SectionType { get; set; }
    public string SeatRow { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public decimal Price { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public string? VoucherId { get; set; }
}