namespace ChampionsLeague.Domain.Entities;

/// <summary>
/// A single seat reservation for a specific Match in a specific Sector.
/// A unique VoucherId (GUID) is generated at purchase time and e-mailed to the buyer.
/// </summary>
public class Ticket
{
    public int Id { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public int SectorId { get; set; }
    public Sector Sector { get; set; } = null!;

    /// <summary>Seat number within the sector (1-based sequential assignment).</summary>
    public int SeatNumber { get; set; }

    /// <summary>Price paid — captured at order time so later price changes don't alter history.</summary>
    public decimal PricePaid { get; set; }

    /// <summary>Globally unique voucher code printed on the PDF ticket / e-mailed to the buyer.</summary>
    public Guid VoucherId { get; set; } = Guid.NewGuid();

    public TicketStatus Status { get; set; } = TicketStatus.Reserved;

    /// <summary>FK to the OrderLine that created this ticket.</summary>
    public int OrderLineId { get; set; }
    public OrderLine OrderLine { get; set; } = null!;
}

/// <summary>Lifecycle states of a ticket as it moves through the purchase flow.</summary>
public enum TicketStatus
{
    Reserved,   // added to order but not yet confirmed
    Paid,       // purchase confirmed
    Cancelled   // cancelled within the free-cancellation window
}
