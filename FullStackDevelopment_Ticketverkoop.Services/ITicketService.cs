using FullStackDevelopment_Ticketverkoop.Domain.Entities;

namespace FullStackDevelopment_Ticketverkoop.Services
{
    /// <summary>
    /// Business logic contract for ticket operations.
    /// All business rules (max 4 per person, same-day restriction, cancellation window)
    /// are enforced here — not in the controller.
    /// </summary>
    public interface ITicketService
    {
        Task<(bool Success, string Message)> PurchaseTicketsAsync(
            string userId, int matchId, int sectionTypeId, int quantity);

        Task<(bool Success, string Message)> CancelTicketAsync(
            string userId, int ticketId);

        Task<IEnumerable<Ticket>> GetUserHistoryAsync(string userId);
    }
}