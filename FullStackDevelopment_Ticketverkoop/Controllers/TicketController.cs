using AutoMapper;
using FullStackDevelopment_Ticketverkoop.Services;
using FullStackDevelopment_Ticketverkoop.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FullStackDevelopment_Ticketverkoop.Web.Controllers
{
    /// <summary>
    /// Handles ticket purchasing and cancellation.
    /// All actions require the user to be logged in ([Authorize]).
    /// </summary>
    [Authorize]
    public class TicketController : Controller
    {
        private readonly ITicketService _ticketService;
        private readonly UserManager<Microsoft.AspNetCore.Identity.IdentityUser> _userManager;
        private readonly IMapper _mapper;

        public TicketController(
            ITicketService ticketService,
            UserManager<Microsoft.AspNetCore.Identity.IdentityUser> userManager,
            IMapper mapper)
        {
            _ticketService = ticketService;
            _userManager = userManager;
            _mapper = mapper;
        }

        /// <summary>
        /// POST: Purchases tickets after all business rules pass in TicketService.
        /// Redirects to order history on success.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Purchase(CartViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Details", "Match", new { id = model.MatchId });

            var userId = _userManager.GetUserId(User)!;
            var (success, message) = await _ticketService
                .PurchaseTicketsAsync(userId, model.MatchId, model.SectionTypeId, model.Quantity);

            TempData[success ? "Success" : "Error"] = message;
            return success
                ? RedirectToAction("History")
                : RedirectToAction("Details", "Match", new { id = model.MatchId });
        }

        /// <summary>Shows the logged-in user's full ticket purchase history.</summary>
        public async Task<IActionResult> History()
        {
            var userId = _userManager.GetUserId(User)!;
            var tickets = await _ticketService.GetUserHistoryAsync(userId);
            var viewModels = _mapper.Map<IEnumerable<TicketHistoryViewModel>>(tickets);
            return View(viewModels);
        }

        /// <summary>POST: Cancels a ticket if the cancellation window is still open.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int ticketId)
        {
            var userId = _userManager.GetUserId(User)!;
            var (success, message) = await _ticketService.CancelTicketAsync(userId, ticketId);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction("History");
        }
    }
}