using AutoMapper;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ChampionsLeague.Domain.Entities;

namespace ChampionsLeague.Web.Controllers.Api;

/// <summary>
/// REST API for the Champions League Ticket Portal.
/// Used with Swagger UI (/swagger) and Postman for testing.
/// Curriculum section 4: Swagger en Postman voor API-testing.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TicketsApiController : ControllerBase
{
    private readonly IMatchRepository  _matches;
    private readonly IClubRepository   _clubs;
    private readonly ITicketRepository _tickets;
    private readonly ITicketService    _ticketService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public TicketsApiController(
        IMatchRepository  matches,
        IClubRepository   clubs,
        ITicketRepository tickets,
        ITicketService    ticketService,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _matches       = matches;
        _clubs         = clubs;
        _tickets       = tickets;
        _ticketService = ticketService;
        _userManager   = userManager;
        _mapper        = mapper;
    }

    // ── GET /api/tickets/matches ──────────────────────────────────────────

    /// <summary>Returns all matches with club and stadium information.</summary>
    [HttpGet("matches")]
    [ProducesResponseType(typeof(IEnumerable<MatchListItemVM>), 200)]
    public async Task<IActionResult> GetMatches()
    {
        var matches = await _matches.GetAllWithClubsAsync();
        var vms     = _mapper.Map<IEnumerable<MatchListItemVM>>(matches);
        return Ok(vms);
    }

    // ── GET /api/tickets/matches/{id} ─────────────────────────────────────

    /// <summary>Returns a single match by ID.</summary>
    [HttpGet("matches/{id:int}")]
    [ProducesResponseType(typeof(MatchListItemVM), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMatch(int id)
    {
        var all   = await _matches.GetAllWithClubsAsync();
        var match = all.FirstOrDefault(m => m.Id == id);
        if (match is null) return NotFound(new { error = $"Match {id} not found." });
        return Ok(_mapper.Map<MatchListItemVM>(match));
    }

    // ── GET /api/tickets/clubs ────────────────────────────────────────────

    /// <summary>Returns all clubs with their stadium and sector information.</summary>
    [HttpGet("clubs")]
    [ProducesResponseType(typeof(IEnumerable<ClubCardVM>), 200)]
    public async Task<IActionResult> GetClubs()
    {
        var clubs = await _clubs.GetAllWithStadiumsAsync();
        var vms   = _mapper.Map<IEnumerable<ClubCardVM>>(clubs);
        return Ok(vms);
    }

    // ── GET /api/tickets/availability/{matchId}/{sectorId} ────────────────

    /// <summary>Returns available seat numbers for a given match and sector.</summary>
    [HttpGet("availability/{matchId:int}/{sectorId:int}")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetAvailability(int matchId, int sectorId)
    {
        var seats = await _ticketService.GetAvailableSeatsAsync(matchId, sectorId);
        return Ok(new
        {
            matchId,
            sectorId,
            availableSeats  = seats,
            availableCount  = seats.Count()
        });
    }

    // ── GET /api/tickets/mytickets ────────────────────────────────────────

    /// <summary>Returns the authenticated user's ticket history. Requires login.</summary>
    [HttpGet("mytickets")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<TicketHistoryItemVM>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetMyTickets()
    {
        var userId  = _userManager.GetUserId(User)!;
        var tickets = await _tickets.GetUserTicketsAsync(userId);
        var vms     = _mapper.Map<IEnumerable<TicketHistoryItemVM>>(tickets);
        return Ok(vms);
    }

    // ── POST /api/tickets/purchase ────────────────────────────────────────

    /// <summary>
    /// Purchases tickets for a match. Enforces all business rules:
    /// sale window, max 4 per person, no same-day double booking, capacity check.
    /// Requires authentication.
    /// </summary>
    [HttpPost("purchase")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Purchase([FromBody] ApiPurchaseRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = _userManager.GetUserId(User)!;
        var result = await _ticketService.PurchaseAsync(new PurchaseRequest(
            userId, req.MatchId, req.SectorId, req.Quantity, req.UnitPrice));

        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new
        {
            success      = true,
            ticketCount  = result.Tickets?.Count() ?? 0,
            vouchers     = result.Tickets?.Select(t => t.VoucherId.ToString("D"))
        });
    }

    // ── POST /api/tickets/cancel/{ticketId} ───────────────────────────────

    /// <summary>
    /// Cancels a ticket. Free cancellation up to 7 days before match.
    /// Requires authentication — you can only cancel your own tickets.
    /// </summary>
    [HttpPost("cancel/{ticketId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Cancel(int ticketId)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _ticketService.CancelAsync(ticketId, userId);

        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { success = true, message = "Ticket cancelled successfully." });
    }
}

/// <summary>Request body for the purchase endpoint.</summary>
public class ApiPurchaseRequest
{
    public int     MatchId    { get; set; }
    public int     SectorId   { get; set; }
    public int     Quantity   { get; set; }
    public decimal UnitPrice  { get; set; }
}
