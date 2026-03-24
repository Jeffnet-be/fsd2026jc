using FullStackDevelopment_Ticketverkoop.Domain.Enums;

namespace FullStackDevelopment_Ticketverkoop.Domain;

/// <summary>
/// Represents a single purchasable seat for a specific match and section.
/// A voucher code is generated upon purchase and emailed to the buyer.
/// </summary>
public class Ticket
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public Match? Match { get; set; }

    public int SectionTypeId { get; set; }
    public SectionType? SectionType { get; set; }

    /// <summary>Seat row identifier within the section.</summary>
    public string SeatRow { get; set; } = string.Empty;

    /// <summary>Seat number within the row.</summary>
    public int SeatNumber { get; set; }

    public decimal Price { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Available;

    /// <summary>
    /// Unique voucher code generated at checkout.
    /// Null until the ticket is purchased.
    /// </summary>
    public string? VoucherId { get; set; }

    public string? BuyerUserId { get; set; }
    public int? OrderLineId { get; set; }
    public OrderLine? OrderLine { get; set; }
}