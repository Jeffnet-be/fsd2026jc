using FullStackDevelopment_Ticketverkoop.Data.Repositories;
using FullStackDevelopment_Ticketverkoop.Domain.Entities;
using FullStackDevelopment_Ticketverkoop.Domain.Enums;

namespace FullStackDevelopment_Ticketverkoop.Services;

/// <summary>
/// Implements all ticket purchase business rules:
/// - Tickets available only from 1 month before match
/// - Max 4 tickets per person per match
/// - No two different matches on same day
/// - Capacity cannot be exceeded
/// - Free cancellation up to 1 week before match
/// - Voucher is generated and emailed on purchase
/// </summary>
public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IEmailService _emailService;

    public TicketService(
        ITicketRepository ticketRepo,
        IOrderRepository orderRepo,
        IEmailService emailService)
    {
        _ticketRepo = ticketRepo;
        _orderRepo = orderRepo;
        _emailService = emailService;
    }

    /// <summary>
    /// Attempts to purchase the requested number of tickets.
    /// Returns a success flag and a human-readable message for display in the view.
    /// </summary>
    public async Task<(bool Success, string Message)> PurchaseTicketsAsync(
        string userId, int matchId, int sectionTypeId, int quantity)
    {
        // Business rule: tickets available from 1 month before match
        // (The match date is loaded via the tickets, so we fetch one ticket first)
        var available = (await _ticketRepo
            .GetAvailableByMatchAndSectionAsync(matchId, sectionTypeId)).ToList();

        if (!available.Any())
            return (false, "No tickets available for this section.");

        var matchDate = available.First().Match?.MatchDate ?? DateTime.MaxValue;

        if (DateTime.UtcNow < matchDate.AddMonths(-1))
            return (false, "Ticket sales for this match have not opened yet (opens 1 month before).");

        if (quantity < 1 || quantity > 4)
            return (false, "You can purchase between 1 and 4 tickets.");

        // Business rule: max 4 total per user per match
        var alreadyBought = await _ticketRepo.CountSoldByUserForMatchAsync(userId, matchId);
        if (alreadyBought + quantity > 4)
            return (false, $"You already have {alreadyBought} ticket(s) for this match. Max 4 total.");

        // Business rule: no two different matches on same day
        var sameDay = await _ticketRepo.HasUserBoughtOnSameDayAsync(userId, matchDate, matchId);
        if (sameDay)
            return (false, "You already have a ticket for another match on this day.");

        // Business rule: capacity check (no overbooking)
        if (available.Count < quantity)
            return (false, $"Only {available.Count} seat(s) remaining in this section.");

        // Reserve the seats and generate vouchers
        var toSell = available.Take(quantity).ToList();
        var order = new Order { UserId = userId, TotalPrice = 0 };

        foreach (var ticket in toSell)
        {
            ticket.Status = TicketStatus.Sold;
            ticket.BuyerUserId = userId;
            ticket.VoucherId = Guid.NewGuid().ToString("N").ToUpper()[..10];
            order.TotalPrice += ticket.Price;
            order.OrderLines.Add(new OrderLine
            {
                TicketId = ticket.Id,
                UnitPrice = ticket.Price
            });
        }

        await _ticketRepo.UpdateRangeAsync(toSell);
        await _orderRepo.CreateAsync(order);

        // Send voucher email (fire-and-forget style — don't block purchase on email failure)
        _ = _emailService.SendVoucherEmailAsync(userId, toSell);

        return (true, $"Purchase successful! {quantity} ticket(s) confirmed. Check your email for your voucher(s).");
    }

    /// <summary>
    /// Cancels a ticket if the match is still more than 1 week away.
    /// </summary>
    public async Task<(bool Success, string Message)> CancelTicketAsync(
        string userId, int ticketId)
    {
        var ticket = await _ticketRepo.GetByIdAsync(ticketId);
        if (ticket is null || ticket.BuyerUserId != userId)
            return (false, "Ticket not found or does not belong to you.");

        if (ticket.Status != TicketStatus.Sold)
            return (false, "Only sold tickets can be cancelled.");

        if (ticket.Match is null)
            return (false, "Match data not found.");

        // Business rule: free cancellation up to 1 week before match
        if (DateTime.UtcNow > ticket.Match.MatchDate.AddDays(-7))
            return (false, "Cancellation is no longer possible (less than 1 week before the match).");

        ticket.Status = TicketStatus.Cancelled;
        ticket.BuyerUserId = null;
        ticket.VoucherId = null;
        await _ticketRepo.UpdateAsync(ticket);

        return (true, "Your ticket has been cancelled successfully.");
    }

    public async Task<IEnumerable<Ticket>> GetUserHistoryAsync(string userId) =>
        await _ticketRepo.GetByUserIdAsync(userId);
}