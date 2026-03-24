using AutoMapper;
using FullStackDevelopment_Ticketverkoop.Data.Repositories;
using FullStackDevelopment_Ticketverkoop.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FullStackDevelopment_Ticketverkoop.Web.Controllers;

/// <summary>
/// Displays the match calendar with optional filtering by club.
/// Also shows stadium section information per club.
/// </summary>
public class MatchController : Controller
{
    private readonly IMatchRepository _matchRepo;
    private readonly IMapper _mapper;

    public MatchController(IMatchRepository matchRepo, IMapper mapper)
    {
        _matchRepo = matchRepo;
        _mapper = mapper;
    }

    /// <summary>
    /// Displays all matches, optionally filtered by club ID.
    /// The filter is passed as a query string: /Match?clubId=2
    /// </summary>
    public async Task<IActionResult> Index(int? clubId)
    {
        var matches = clubId.HasValue
            ? await _matchRepo.GetByClubAsync(clubId.Value)
            : await _matchRepo.GetAllAsync();

        var viewModels = _mapper.Map<IEnumerable<MatchViewModel>>(matches);
        ViewBag.SelectedClubId = clubId;
        return View(viewModels);
    }

    /// <summary>Shows match details with available sections and seat counts.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var match = await _matchRepo.GetByIdWithDetailsAsync(id);
        if (match is null) return NotFound();
        return View(match);
    }
}