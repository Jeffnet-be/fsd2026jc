namespace ChampionsLeague.Domain.Entities;

/// <summary>
/// Order header — groups all lines purchased in a single checkout session.
/// Business rule: a user may not have tickets for two different matches on the same day,
/// enforced in IOrderRepository.UserHasMatchOnDayAsync and checked in TicketService.
/// </summary>
public class Order
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>
    /// Sum of all OrderLine amounts — captured at purchase time for history accuracy.
    /// </summary>
    public decimal TotalAmount { get; set; }

    public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
}

/// <summary>Lifecycle states of an order.</summary>
public enum OrderStatus
{
    Pending,    // in cart / not yet confirmed
    Paid,       // payment confirmed
    Cancelled   // all lines cancelled
}
