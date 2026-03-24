using ChampionsLeague.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Handles the checkout flow: Review cart → Confirm purchase → Show confirmation.
/// [Authorize] ensures only authenticated users can reach these actions.
/// TicketService is the single entry point for all business-rule enforcement.
/// </summary>
[Authorize]
public class CheckoutController : Controller
{
    private const string CartSessionKey = "CART";
    private readonly ITicketService _ticketService;

    public CheckoutController(ITicketService ticketService)
        => _ticketService = ticketService;

    /// <summary>
    /// GET /Checkout/Review — shows the cart contents before the user confirms.
    /// Redirects to Cart/Index if the cart is empty.
    /// </summary>
    public IActionResult Review()
    {
        var cart = GetCart();
        if (!cart.Items.Any())
            return RedirectToAction("Index", "Cart");

        return View(cart);
    }

    /// <summary>
    /// POST /Checkout/Confirm — calls TicketService for each cart item.
    /// On success: clears cart and redirects to Confirmation.
    /// On any business-rule failure: stores errors in TempData and returns to Review.
    /// ValidateAntiForgeryToken prevents CSRF attacks on this financial action.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm()
    {
        var cart   = GetCart();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var errors = new List<string>();

        foreach (var item in cart.Items)
        {
            var result = await _ticketService.PurchaseAsync(new PurchaseRequest(
                UserId    : userId,
                MatchId   : item.MatchId,
                SectorId  : item.SectorId,
                Quantity  : item.Quantity,
                UnitPrice : item.UnitPrice
            ));

            if (!result.Success)
                errors.Add($"{item.MatchDescription} — {result.ErrorMessage}");
        }

        if (errors.Any())
        {
            TempData["Errors"] = string.Join("|", errors);
            return RedirectToAction(nameof(Review));
        }

        // Clear cart only after all lines succeed
        HttpContext.Session.Remove(CartSessionKey);
        TempData["Success"] = "Your tickets have been purchased! Check your email for vouchers.";
        return RedirectToAction(nameof(Confirmation));
    }

    /// <summary>Confirmation page shown after a successful purchase.</summary>
    public IActionResult Confirmation() => View();

    // ── Helper ────────────────────────────────────────────────────────────
    private CartVM GetCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json)) return new CartVM();
        return JsonSerializer.Deserialize<CartVM>(json) ?? new CartVM();
    }
}
