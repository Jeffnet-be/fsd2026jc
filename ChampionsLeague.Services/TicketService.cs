using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;

namespace ChampionsLeague.Services;

// ── DTOs / result records ──────────────────────────────────────────────────

/// <summary>Input data for a ticket purchase request.</summary>
public record PurchaseRequest(
    string  UserId,
    int     MatchId,
    int     SectorId,
    int     Quantity,
    decimal UnitPrice);

/// <summary>
/// Result object — success or a business-rule violation message.
/// Using a result object instead of exceptions keeps controllers clean:
/// expected failures are not exceptional events.
/// </summary>
public record PurchaseResult(
    bool                    Success,
    string?                 ErrorMessage = null,
    IEnumerable<Ticket>?    Tickets      = null);

/// <summary>Result for cancellation attempts.</summary>
public record CancelResult(bool Success, string? ErrorMessage = null);

// ── Interface ──────────────────────────────────────────────────────────────

public interface ITicketService
{
    Task<PurchaseResult> PurchaseAsync(PurchaseRequest request);
    Task<CancelResult>   CancelAsync(int ticketId, string userId);
    Task<IEnumerable<int>> GetAvailableSeatsAsync(int matchId, int sectorId);
}

// ── Implementation ─────────────────────────────────────────────────────────

public class TicketService : ITicketService
{
    private readonly ITicketRepository       _tickets;
    private readonly IOrderRepository        _orders;
    private readonly IMatchRepository        _matches;
    private readonly ISeasonTicketRepository _seasonTickets;
    private readonly IClubRepository         _clubs;
    private readonly IEmailService           _email;
    private readonly UserManager<ApplicationUser> _userManager;

    public TicketService(
        ITicketRepository        tickets,
        IOrderRepository         orders,
        IMatchRepository         matches,
        ISeasonTicketRepository  seasonTickets,
        IClubRepository          clubs,
        IEmailService            email,
        UserManager<ApplicationUser> userManager)
    {
        _tickets       = tickets;
        _orders        = orders;
        _matches       = matches;
        _seasonTickets = seasonTickets;
        _clubs         = clubs;
        _email         = email;
        _userManager   = userManager;
    }

    /// <inheritdoc/>
    public async Task<PurchaseResult> PurchaseAsync(PurchaseRequest req)
    {
        // ── Rule 1: match must exist ──────────────────────────────────
        var matches = await _matches.GetAllWithClubsAsync();
        var match   = matches.FirstOrDefault(m => m.Id == req.MatchId);
        if (match is null)
            return new PurchaseResult(false, "Match not found.");

        // ── Rule 2: sale window must be open ──────────────────────────
        if (!match.IsSaleOpen)
            return new PurchaseResult(false,
                $"Ticket sale opens on {match.MatchDate.AddMonths(-1):dd/MM/yyyy}.");

        // ── Rule 3: quantity 1-4 per match per person ─────────────────
        if (req.Quantity is < 1 or > 4)
            return new PurchaseResult(false,
                "You may purchase between 1 and 4 tickets per match.");

        // ── Rule 4: no two matches on the same calendar day ───────────
        if (await _orders.UserHasMatchOnDayAsync(req.UserId, match.MatchDate))
            return new PurchaseResult(false,
                "You already have a ticket for another match on this day.");

        // ── Rule 5: capacity check (excl. season-ticket seats) ────────
        var seasonSeats = (await _seasonTickets.GetSeasonReservedSeatsAsync(req.SectorId)).ToHashSet();
        var soldSeats   = (await _tickets.GetReservedSeatsAsync(req.MatchId, req.SectorId)).ToHashSet();
        var allTaken    = seasonSeats.Union(soldSeats).ToHashSet();

        var club   = await _clubs.GetWithStadiumAndSectorsAsync(match.HomeClubId);
        var sector = club?.Stadium?.Sectors.FirstOrDefault(s => s.Id == req.SectorId);
        if (sector is null)
            return new PurchaseResult(false, "Sector not found.");

        var freeSeats = Enumerable.Range(1, sector.Capacity)
                                  .Where(s => !allTaken.Contains(s))
                                  .Take(req.Quantity)
                                  .ToList();

        if (freeSeats.Count < req.Quantity)
            return new PurchaseResult(false,
                $"Only {freeSeats.Count} seat(s) available in this sector.");

        // ── Create Order + OrderLine + Tickets ────────────────────────
        var order = new Order
        {
            UserId      = req.UserId,
            Status      = OrderStatus.Paid,
            CreatedAt   = DateTime.UtcNow,
            TotalAmount = req.UnitPrice * req.Quantity
        };
        await _orders.AddAsync(order);
        await _orders.SaveChangesAsync();

        var createdTickets = freeSeats.Select(seat => new Ticket
        {
            MatchId    = req.MatchId,
            SectorId   = req.SectorId,
            SeatNumber = seat,
            PricePaid  = req.UnitPrice,
            Status     = TicketStatus.Paid,
            VoucherId  = Guid.NewGuid()
        }).ToList();

        var line = new OrderLine
        {
            OrderId   = order.Id,
            MatchId   = req.MatchId,
            SectorId  = req.SectorId,
            Quantity  = req.Quantity,
            UnitPrice = req.UnitPrice,
            Tickets   = createdTickets
        };

        order.OrderLines.Add(line);
        await _orders.SaveChangesAsync();

        // ── Send voucher email with REAL user email address ───────────
        _ = SendVoucherEmailAsync(req.UserId, createdTickets, match);

        return new PurchaseResult(true, null, createdTickets);
    }

    /// <inheritdoc/>
    public async Task<CancelResult> CancelAsync(int ticketId, string userId)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId);
        if (ticket is null)
            return new CancelResult(false, "Ticket not found.");

        var match = (await _matches.GetAllWithClubsAsync())
                        .FirstOrDefault(m => m.Id == ticket.MatchId);

        if (match is null || !match.IsCancellable)
            return new CancelResult(false,
                "Free cancellation is only available up to 7 days before kick-off.");

        ticket.Status = TicketStatus.Cancelled;
        _tickets.Update(ticket);
        await _tickets.SaveChangesAsync();

        return new CancelResult(true);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<int>> GetAvailableSeatsAsync(int matchId, int sectorId)
    {
        var matches = await _matches.GetAllWithClubsAsync();
        var match   = matches.FirstOrDefault(m => m.Id == matchId);
        if (match is null) return Enumerable.Empty<int>();

        var club   = await _clubs.GetWithStadiumAndSectorsAsync(match.HomeClubId);
        var sector = club?.Stadium?.Sectors.FirstOrDefault(s => s.Id == sectorId);
        if (sector is null) return Enumerable.Empty<int>();

        var seasonSeats = (await _seasonTickets.GetSeasonReservedSeatsAsync(sectorId)).ToHashSet();
        var soldSeats   = (await _tickets.GetReservedSeatsAsync(matchId, sectorId)).ToHashSet();
        var allTaken    = seasonSeats.Union(soldSeats).ToHashSet();

        return Enumerable.Range(1, sector.Capacity).Where(s => !allTaken.Contains(s));
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task SendVoucherEmailAsync(
        string userId, IEnumerable<Ticket> tickets, Match match)
    {
        // Look up the real user email from Identity
        var user      = await _userManager.FindByIdAsync(userId);
        var realEmail = user?.Email ?? $"{userId}@unknown.com";
        var firstName = user?.FirstName ?? "Supporter";

        var lines = string.Join("<br/>", tickets.Select(t =>
            $"&bull; Seat <strong>{t.SeatNumber}</strong> &mdash; " +
            $"Voucher: <code>{t.VoucherId:D}</code>"));

        await _email.SendAsync(
            to      : realEmail,
            subject : $"Your tickets: {match.HomeClub?.Name} vs {match.AwayClub?.Name}",
            htmlBody: $@"
<p>Hello {firstName},</p>
<p>Thank you for your purchase! Your tickets for
<strong>{match.HomeClub?.Name} vs {match.AwayClub?.Name}</strong>
on {match.MatchDate:dd MMMM yyyy} are confirmed.</p>
<p>{lines}</p>
<p>Present your voucher code at the stadium gate on match day.</p>
<p>CL Tickets Portal</p>"
        );
    }
}
