namespace ChampionsLeague.Domain.Entities;

/// <summary>
/// One line in an Order: the purchase of N tickets for a specific Match + Sector combination.
/// Each OrderLine spawns one Ticket entity per quantity unit.
/// </summary>
public class OrderLine
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public int SectorId { get; set; }
    public Sector Sector { get; set; } = null!;

    /// <summary>Number of tickets purchased in this line (1–4, enforced by business rule).</summary>
    public int Quantity { get; set; }

    /// <summary>Unit price at time of purchase — immutable after confirmation.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Computed total — excluded from DB column (EF Ignore in Fluent API).</summary>
    public decimal LineTotal => Quantity * UnitPrice;

    /// <summary>Individual ticket records generated from this line (one per seat).</summary>
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
