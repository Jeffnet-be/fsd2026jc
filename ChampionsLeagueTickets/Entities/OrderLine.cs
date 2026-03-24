namespace FullStackDevelopment_Ticketverkoop.Domain.Entities;

/// <summary>
/// A single line in an order, linking the order to one ticket.
/// </summary>
public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public decimal UnitPrice { get; set; }
}