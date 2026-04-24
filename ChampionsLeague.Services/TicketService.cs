using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;

namespace ChampionsLeague.Services;

public record PurchaseRequest(string UserId, int MatchId, int SectorId, int Quantity, decimal UnitPrice);

/// <summary>
/// Result of a purchase attempt.
/// Email is intentionally NOT sent here — the caller (CheckoutController) sends
/// emails only after ALL cart items have been successfully saved, so a partial
/// failure never results in confirmation emails for items that weren't booked.
/// </summary>
/// <summary>
/// Foutcodes voor aankoop-fouten.
/// De Web-laag (CheckoutController) vertaalt deze codes naar de juiste taal.
/// Zo blijft de Services-laag taalongevoelig.
/// </summary>
public enum PurchaseErrorCode
{
    None,
    MatchNotFound,
    SaleNotOpen,
    MaxTicketsExceeded,
    MinQuantity,
    SameDayMatch,
    SectorNotFound,
    NotEnoughSeats
}

public record PurchaseResult(
    bool                  Success,
    string?               ErrorMessage = null,
    IEnumerable<Ticket>?  Tickets      = null,
    PurchaseErrorCode     ErrorCode    = PurchaseErrorCode.None,
    int                   SeatsLeft    = 0,
    int                   AlreadyOwned = 0,
    DateTime              SaleOpensOn  = default);

public record CancelResult(bool Success, string? ErrorMessage = null);

public interface ITicketService
{
    Task<PurchaseResult> PurchaseAsync(PurchaseRequest request);
    Task<CancelResult>   CancelAsync(int ticketId, string userId);
    Task<IEnumerable<int>> GetAvailableSeatsAsync(int matchId, int sectorId);
}

public class TicketService : ITicketService
{
    private readonly ITicketRepository       _tickets;
    private readonly IOrderRepository        _orders;
    private readonly IMatchRepository        _matches;
    private readonly ISeasonTicketRepository _seasonTickets;
    private readonly IClubRepository         _clubs;
    private readonly UserManager<ApplicationUser> _userManager;

    // NOTE: IEmailService is intentionally NOT injected here.
    // Emails are sent by CheckoutController AFTER all items succeed,
    // preventing confirmation emails for partially-failed orders.

    public TicketService(
        ITicketRepository        tickets,
        IOrderRepository         orders,
        IMatchRepository         matches,
        ISeasonTicketRepository  seasonTickets,
        IClubRepository          clubs,
        UserManager<ApplicationUser> userManager)
    {
        _tickets       = tickets;
        _orders        = orders;
        _matches       = matches;
        _seasonTickets = seasonTickets;
        _clubs         = clubs;
        _userManager   = userManager;
    }

    public async Task<PurchaseResult> PurchaseAsync(PurchaseRequest req)
    {
        // ── Rule 1: match must exist ──────────────────────────────────
        var matches = await _matches.GetAllWithClubsAsync();
        var match   = matches.FirstOrDefault(m => m.Id == req.MatchId);
        if (match is null)
            return new PurchaseResult(false, "Match not found.", ErrorCode: PurchaseErrorCode.MatchNotFound);

        // ── Rule 2: sale window open ──────────────────────────────────
        if (!match.IsSaleOpen)
            return new PurchaseResult(false,
                "sale_not_open", ErrorCode: PurchaseErrorCode.SaleNotOpen, SaleOpensOn: match.MatchDate.AddMonths(-1));

        // ── Rule 3: max 4 tickets per person per match ────────────────
        var alreadyOwned = await _tickets.GetUserTicketCountForMatchAsync(req.UserId, req.MatchId);
        if (alreadyOwned + req.Quantity > 4)
            return new PurchaseResult(false,
                "max_exceeded", ErrorCode: PurchaseErrorCode.MaxTicketsExceeded, AlreadyOwned: alreadyOwned);

        if (req.Quantity < 1)
            return new PurchaseResult(false, "min_quantity", ErrorCode: PurchaseErrorCode.MinQuantity);

        // ── Rule 4: no two matches on the same day ────────────────────
        if (await _orders.UserHasMatchOnDayAsync(req.UserId, match.MatchDate))
            return new PurchaseResult(false,
                "same_day", ErrorCode: PurchaseErrorCode.SameDayMatch);

        // ── Rule 5: capacity check (excl. season seats) ───────────────
        var seasonSeats = (await _seasonTickets.GetSeasonReservedSeatsAsync(req.SectorId)).ToHashSet();
        var soldSeats   = (await _tickets.GetReservedSeatsAsync(req.MatchId, req.SectorId)).ToHashSet();
        var allTaken    = seasonSeats.Union(soldSeats).ToHashSet();

        var club   = await _clubs.GetWithStadiumAndSectorsAsync(match.HomeClubId);
        var sector = club?.Stadium?.Sectors.FirstOrDefault(s => s.Id == req.SectorId);
        if (sector is null)
            return new PurchaseResult(false, "sector_not_found", ErrorCode: PurchaseErrorCode.SectorNotFound);

        var freeSeats = Enumerable.Range(1, sector.Capacity)
                                  .Where(s => !allTaken.Contains(s))
                                  .Take(req.Quantity)
                                  .ToList();

        if (freeSeats.Count < req.Quantity)
            return new PurchaseResult(false,
                "not_enough_seats", ErrorCode: PurchaseErrorCode.NotEnoughSeats, SeatsLeft: freeSeats.Count);

        // ── Save Order + OrderLine + Tickets ──────────────────────────
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

        // ── No email here — CheckoutController sends after ALL items succeed ──
        return new PurchaseResult(true, null, createdTickets);
    }

    public async Task<CancelResult> CancelAsync(int ticketId, string userId)
    {
        var ticket = await _tickets.GetByIdTrackedAsync(ticketId);
        if (ticket is null)
            return new CancelResult(false, "Ticket not found.");

        var userTickets = await _tickets.GetUserTicketsAsync(userId);
        if (!userTickets.Any(t => t.Id == ticketId))
            return new CancelResult(false, "You can only cancel your own tickets.");

        if (ticket.Status == TicketStatus.Cancelled)
            return new CancelResult(false, "This ticket is already cancelled.");

        var match = (await _matches.GetAllWithClubsAsync())
                        .FirstOrDefault(m => m.Id == ticket.MatchId);

        if (match is null || !match.IsCancellable)
            return new CancelResult(false,
                "Free cancellation is only available up to 7 days before kick-off.");

        ticket.Status = TicketStatus.Cancelled;
        await _tickets.SaveChangesAsync();

        return new CancelResult(true);
    }

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
}
