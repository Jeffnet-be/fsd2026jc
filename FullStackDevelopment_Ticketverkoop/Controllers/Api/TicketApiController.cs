using FullStackDevelopment_Ticketverkoop.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FullStackDevelopment_Ticketverkoop.Web.Controllers.Api;

/// <summary>
/// RESTful API controller for ticket data.
/// Documented via Swagger — accessible at /swagger in development.
/// Used to demonstrate API testing with Postman as per the curriculum.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TicketApiController : ControllerBase
{
    private readonly ITicketRepository _ticketRepo;

    public TicketApiController(ITicketRepository ticketRepo) => _ticketRepo = ticketRepo;

    /// <summary>Returns all sold tickets for a specific user (for Postman demo).</summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var tickets = await _ticketRepo.GetByUserIdAsync(userId);
        return Ok(tickets.Select(t => new
        {
            t.Id,
            Match = $"{t.Match?.HomeClub?.Name} vs {t.Match?.AwayClub?.Name}",
            t.Match?.MatchDate,
            Section = t.SectionType?.Name,
            t.SeatRow,
            t.SeatNumber,
            t.Price,
            t.VoucherId,
            Status = t.Status.ToString()
        }));
    }
}