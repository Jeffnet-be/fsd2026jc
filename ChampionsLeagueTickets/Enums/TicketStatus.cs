namespace FullStackDevelopment_Ticketverkoop.Domain.Enums;

/// <summary>
/// Describes the current state of a ticket.
/// </summary>
public enum TicketStatus
{
    Available,
    Reserved,   // In a cart, not yet paid
    Sold,
    Cancelled
}