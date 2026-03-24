using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Services;

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

/// <summary>
/// Service contract for the ticket purchase flow.
/// Controllers depend on this interface — never on the concrete class — enabling mocking in tests.
/// </summary>
public interface ITicketService
{
    /// <summary>Attempts to purchase seats; enforces all business rules.</summary>
    Task<PurchaseResult> PurchaseAsync(PurchaseRequest request);

    /// <summary>Cancels a ticket within the free-cancellation window.</summary>
    Task<CancelResult> CancelAsync(int ticketId, string userId);

    /// <summary>Returns seat numbers still available in a sector for a match.</summary>
    Task<IEnumerable<int>> GetAvailableSeatsAsync(int matchId, int sectorId);
}

// ── Implementation ─────────────────────────────────────────────────────────

/// <summary>
/// Orchestrates the complete ticket purchase flow.
/// All business rules from the project spec are enforced here — not in controllers or repositories.
/// This is the Service Layer of the multilayer architecture (section 9.1 of the curriculum).
/// </summary>
public class TicketService : ITicketService
{
    private readonly ITicketRepository      _tickets;
    private readonly IOrderRepository       _orders;
    private readonly IMatchRepository       _matches;
    private readonly ISeasonTicketRepository _seasonTickets;
    private readonly IClubRepository        _clubs;
    private readonly IEmailService          _email;

    public TicketService(
        ITicketRepository       tickets,
        IOrderRepository        orders,
        IMatchRepository        matches,
        ISeasonTicketRepository seasonTickets,
        IClubRepository         clubs,
        IEmailService           email)
    {
        _tickets       = tickets;
        _orders        = orders;
        _matches       = matches;
        _seasonTickets = seasonTickets;
        _clubs         = clubs;
        _email         = email;
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

        // Find the sector capacity from the club's stadium
        var club       = await _clubs.GetWithStadiumAndSectorsAsync(match.HomeClubId);
        var sector     = club?.Stadium?.Sectors.FirstOrDefault(s => s.Id == req.SectorId);
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
            Status      = OrderStatus.Paid,           // simplified: direct payment
            CreatedAt   = DateTime.UtcNow,
            TotalAmount = req.UnitPrice * req.Quantity
        };
        await _orders.AddAsync(order);
        await _orders.SaveChangesAsync();             // flush to get order.Id

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

        // ── Send voucher email (fire-and-forget for demo) ─────────────
        _ = SendVoucherEmailAsync(req.UserId, createdTickets, match);

        return new PurchaseResult(true, null, createdTickets);
    }

    /// <inheritdoc/>
    public async Task<CancelResult> CancelAsync(int ticketId, string userId)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId);
        if (ticket is null)
            return new CancelResult(false, "Ticket not found.");

        // Load match to check cancellation window
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

        var club    = await _clubs.GetWithStadiumAndSectorsAsync(match.HomeClubId);
        var sector  = club?.Stadium?.Sectors.FirstOrDefault(s => s.Id == sectorId);
        if (sector is null) return Enumerable.Empty<int>();

        var seasonSeats = (await _seasonTickets.GetSeasonReservedSeatsAsync(sectorId)).ToHashSet();
        var soldSeats   = (await _tickets.GetReservedSeatsAsync(matchId, sectorId)).ToHashSet();
        var allTaken    = seasonSeats.Union(soldSeats).ToHashSet();

        return Enumerable.Range(1, sector.Capacity).Where(s => !allTaken.Contains(s));
    }

    // ── Private helpers ───────────────────────────────────────────────────
    private async Task SendVoucherEmailAsync(
        string userId, IEnumerable<Ticket> tickets, Match match)
    {
        var lines = string.Join("<br/>", tickets.Select(t =>
            $"Seat <strong>{t.SeatNumber}</strong> — Voucher: <code>{t.VoucherId:D}</code>"));

        await _email.SendAsync(
            to      : $"{userId}@example.com",   // real app: look up User.Email
            subject : $"Your tickets: {match.HomeClub?.Name} vs {match.AwayClub?.Name}",
            htmlBody: $"<p>Thank you for your purchase!</p><p>{lines}</p>"
        );
    }
}
