using ChampionsLeague.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ChampionsLeague.Domain.Entities;

namespace ChampionsLeague.Web.Controllers.Api;

/// <summary>
/// REST API voor het Champions League Ticket Portal.
/// Gebruikt enkel service-interfaces — geen repository-imports.
/// Mapt service-DTOs naar response-ViewModels hier in de Web-laag.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TicketsApiController : ControllerBase
{
    private readonly IMatchService     _matchService;
    private readonly IClubService      _clubService;
    private readonly ITicketService    _ticketService;
    private readonly IUserTicketService _userTicketService;

    public TicketsApiController(
        IMatchService      matchService,
        IClubService       clubService,
        ITicketService     ticketService,
        IUserTicketService userTicketService)
    {
        _matchService      = matchService;
        _clubService       = clubService;
        _ticketService     = ticketService;
        _userTicketService = userTicketService;
    }

    /// <summary>Alle wedstrijden met club- en stadioninfo.</summary>
    [HttpGet("matches")]
    public async Task<IActionResult> GetMatches()
    {
        var dtos = await _matchService.GetAllAsync();
        // DTO → ViewModel mapping in Web-laag
        var vms = dtos.Select(d => new MatchListItemVM
        {
            Id            = d.Id,
            HomeClubName  = d.HomeClubName,
            AwayClubName  = d.AwayClubName,
            HomeClubBadge = d.HomeClubBadge,
            AwayClubBadge = d.AwayClubBadge,
            StadiumName   = d.StadiumName,
            StadiumCity   = d.StadiumCity,
            MatchDate     = d.MatchDate,
            Phase         = d.Phase,
            IsSaleOpen    = d.IsSaleOpen
        });
        return Ok(vms);
    }

    /// <summary>Één wedstrijd op basis van ID.</summary>
    [HttpGet("matches/{id:int}")]
    public async Task<IActionResult> GetMatch(int id)
    {
        var dtos  = await _matchService.GetAllAsync();
        var match = dtos.FirstOrDefault(m => m.Id == id);
        if (match is null) return NotFound(new { error = $"Match {id} not found." });
        return Ok(new MatchListItemVM
        {
            Id            = match.Id,
            HomeClubName  = match.HomeClubName,
            AwayClubName  = match.AwayClubName,
            HomeClubBadge = match.HomeClubBadge,
            AwayClubBadge = match.AwayClubBadge,
            StadiumName   = match.StadiumName,
            StadiumCity   = match.StadiumCity,
            MatchDate     = match.MatchDate,
            Phase         = match.Phase,
            IsSaleOpen    = match.IsSaleOpen
        });
    }

    /// <summary>Alle clubs met stadion- en sectorinfo.</summary>
    [HttpGet("clubs")]
    public async Task<IActionResult> GetClubs()
    {
        var dtos = await _clubService.GetAllWithStadiumsAsync();
        var vms  = dtos.Select(d => new ClubCardVM
        {
            Id          = d.Id,
            Name        = d.Name,
            BadgeUrl    = d.BadgeUrl,
            StadiumName = d.StadiumName,
            StadiumCity = d.StadiumCity,
            Sectors     = d.Sectors.Select(s => new SectorCardVM
            {
                Id        = s.Id,
                Name      = s.Name,
                Capacity  = s.Capacity,
                BasePrice = s.BasePrice
            }).ToList()
        });
        return Ok(vms);
    }

    /// <summary>Beschikbare stoelnummers voor een wedstrijd en sector.</summary>
    [HttpGet("availability/{matchId:int}/{sectorId:int}")]
    public async Task<IActionResult> GetAvailability(int matchId, int sectorId)
    {
        var seats = await _ticketService.GetAvailableSeatsAsync(matchId, sectorId);
        return Ok(new { matchId, sectorId, availableSeats = seats, availableCount = seats.Count() });
    }

    /// <summary>Actieve tickets van de ingelogde gebruiker.</summary>
    [HttpGet("mytickets")]
    [Authorize]
    public async Task<IActionResult> GetMyTickets()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var dtos   = await _userTicketService.GetActiveTicketsAsync(userId);
        var vms    = dtos.Select(d => new TicketHistoryItemVM
        {
            Id               = d.Id,
            MatchDescription = d.MatchDescription,
            MatchDate        = d.MatchDate,
            SectorName       = d.SectorName,
            SeatNumber       = d.SeatNumber,
            PricePaid        = d.PricePaid,
            VoucherId        = d.VoucherId,
            Status           = d.Status,
            IsCancellable    = d.IsCancellable
        });
        return Ok(vms);
    }

    /// <summary>Koopt tickets (alle businessregels worden gecontroleerd).</summary>
    [HttpPost("purchase")]
    [Authorize]
    public async Task<IActionResult> Purchase([FromBody] ApiPurchaseRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _ticketService.PurchaseAsync(
            new PurchaseRequest(userId, req.MatchId, req.SectorId, req.Quantity, req.UnitPrice));
        if (!result.Success) return BadRequest(new { error = result.ErrorMessage });
        return Ok(new { success = true, ticketCount = result.Tickets?.Count() ?? 0,
            vouchers = result.Tickets?.Select(t => t.VoucherId.ToString("D")) });
    }

    /// <summary>Annuleert een ticket (gratis tot 7 dagen vóór de wedstrijd).</summary>
    [HttpPost("cancel/{ticketId:int}")]
    [Authorize]
    public async Task<IActionResult> Cancel(int ticketId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _ticketService.CancelAsync(ticketId, userId);
        if (!result.Success) return BadRequest(new { error = result.ErrorMessage });
        return Ok(new { success = true, message = "Ticket geannuleerd." });
    }
}

public class ApiPurchaseRequest
{
    public int     MatchId   { get; set; }
    public int     SectorId  { get; set; }
    public int     Quantity  { get; set; }
    public decimal UnitPrice { get; set; }
}
