namespace FullStackDevelopment_Ticketverkoop.Domain.Entities;

/// <summary>
/// Represents a completed purchase order placed by a logged-in user.
/// An order contains one or more order lines (one per ticket).
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalPrice { get; set; }

    public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
}