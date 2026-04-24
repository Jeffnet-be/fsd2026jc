using AutoMapper;
using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers.Api;

/// <summary>
/// REST API voor het Champions League Ticket Portaal.
/// Gedocumenteerd via Swagger UI (/swagger).
///
/// REFACTOR: IMatchRepository, IClubRepository en ITicketRepository zijn
/// vervangen door IMatchService (voor club/wedstrijddata) en ITicketService
/// (voor beschikbaarheid en aankoop/annulatie).
///
/// De API-controller heeft zo géén directe Infrastructure-afhankelijkheden meer.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TicketsApiController : ControllerBase
{
    private readonly IMatchService                _matchService;   // ← vervangt IMatchRepository + IClubRepository
    private readonly ITicketService               _ticketService;  // ← vervangt ITicketRepository voor beschikbaarheid
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccountService              _accountService; // ← vervangt ITicketRepository voor history
    private readonly IMapper                      _mapper;

    public TicketsApiController(
        IMatchService                matchService,
        ITicketService               ticketService,
        IAccountService              accountService,
        UserManager<ApplicationUser> userManager,
        IMapper                      mapper)
    {
        _matchService   = matchService;
        _ticketService  = ticketService;
        _accountService = accountService;
        _userManager    = userManager;
        _mapper         = mapper;
    }

    /// <summary>Geeft alle wedstrijden terug met club- en stadioninformatie.</summary>
    [HttpGet("matches")]
    [ProducesResponseType(typeof(IEnumerable<MatchListItemVM>), 200)]
    public async Task<IActionResult> GetMatches()
    {
        var matches = await _matchService.GetAllWithClubsAsync();
        return Ok(_mapper.Map<IEnumerable<MatchListItemVM>>(matches));
    }

    /// <summary>Geeft één wedstrijd terug op id.</summary>
    [HttpGet("matches/{id:int}")]
    [ProducesResponseType(typeof(MatchListItemVM), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMatch(int id)
    {
        var all   = await _matchService.GetAllWithClubsAsync();
        var match = all.FirstOrDefault(m => m.Id == id);
        if (match is null) return NotFound(new { error = $"Match {id} not found." });
        return Ok(_mapper.Map<MatchListItemVM>(match));
    }

    /// <summary>Geeft alle clubs terug met hun stadion en sectoren.</summary>
    [HttpGet("clubs")]
    [ProducesResponseType(typeof(IEnumerable<ClubCardVM>), 200)]
    public async Task<IActionResult> GetClubs()
    {
        var clubs = await _matchService.GetAllClubsWithStadiumsAsync();
        return Ok(_mapper.Map<IEnumerable<ClubCardVM>>(clubs));
    }

    /// <summary>
    /// Geeft het aantal beschikbare plaatsen terug voor een wedstrijd en sector.
    /// Combineert losse tickets + abonnementen voor een correct getal.
    /// </summary>
    [HttpGet("availability/{matchId:int}/{sectorId:int}")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetAvailability(int matchId, int sectorId)
    {
        var seats = await _ticketService.GetAvailableSeatsAsync(matchId, sectorId);
        return Ok(new
        {
            matchId,
            sectorId,
            availableSeats = seats,
            availableCount = seats.Count()
        });
    }

    /// <summary>Geeft de ticketgeschiedenis van de ingelogde gebruiker terug.</summary>
    [HttpGet("mytickets")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<TicketHistoryItemVM>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetMyTickets()
    {
        var userId  = _userManager.GetUserId(User)!;
        var history = await _accountService.GetUserHistoryAsync(userId);
        return Ok(_mapper.Map<IEnumerable<TicketHistoryItemVM>>(history.Tickets));
    }

    /// <summary>
    /// Koopt tickets voor een wedstrijd.
    /// Handhaaft alle business rules: verkoopvenster, max 4 per persoon,
    /// geen dubbele wedstrijd op dezelfde dag, capaciteitslimiet.
    /// </summary>
    [HttpPost("purchase")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Purchase([FromBody] ApiPurchaseRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = _userManager.GetUserId(User)!;
        var result = await _ticketService.PurchaseAsync(new PurchaseRequest(
            userId, req.MatchId, req.SectorId, req.Quantity, req.UnitPrice));

        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new
        {
            success     = true,
            ticketCount = result.Tickets?.Count() ?? 0,
            vouchers    = result.Tickets?.Select(t => t.VoucherId.ToString("D"))
        });
    }

    /// <summary>Annuleert een ticket. Alleen de eigenaar kan zijn eigen ticket annuleren.</summary>
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

        return Ok(new { success = true, message = "Ticket successfully cancelled." });
    }
}

/// <summary>Request body voor het purchase-endpoint.</summary>
public class ApiPurchaseRequest
{
    public int     MatchId   { get; set; }
    public int     SectorId  { get; set; }
    public int     Quantity  { get; set; }
    public decimal UnitPrice { get; set; }
}
