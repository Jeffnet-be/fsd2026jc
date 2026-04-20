using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Manages the session-based shopping cart.
/// Cart data is stored as JSON in the ASP.NET Core session (configured in Program.cs).
/// AddToCart returns a PartialView so the navbar badge updates via AJAX without a
/// full page reload — demonstrating the unobtrusive-AJAX pattern from curriculum section 10.4.7.
/// </summary>
public class CartController : Controller
{
    private const string CartSessionKey = "CART";

    /// <summary>Displays the full shopping cart page.</summary>
    public IActionResult Index()
    {
        var cart = GetCart();
        return View(cart);
    }

    /// <summary>
    /// Adds one item to the session cart.
    /// Called via AJAX (POST + JSON body) from the match detail page.
    /// Returns a PartialView (_CartSummary) that the JavaScript uses to update the badge.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddToCart([FromBody] CartItemVM item)
    {
        if (item is null)
            return BadRequest(new { error = "Invalid cart item." });

        var cart = GetCart();

        // Enforce max-4-tickets-per-match rule at cart level (second check; TicketService is authoritative)
        var alreadyInCart = cart.Items
            .Where(i => i.MatchId == item.MatchId)
            .Sum(i => i.Quantity);

        if (alreadyInCart + item.Quantity > 4)
            return BadRequest(new { error = "Maximum 4 tickets per match allowed." });

        // Merge with existing line if same match+sector
        var existing = cart.Items
            .FirstOrDefault(i => i.MatchId == item.MatchId && i.SectorId == item.SectorId);

        if (existing is not null)
            existing.Quantity += item.Quantity;
        else
            cart.Items.Add(item);

        SaveCart(cart);

        // Return partial so JS can update only the badge count — no full reload
        return PartialView("_CartSummary", cart);
    }

    /// <summary>Removes a specific match+sector line from the cart.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int matchId, int sectorId)
    {
        var cart = GetCart();
        cart.Items.RemoveAll(i => i.MatchId == matchId && i.SectorId == sectorId);
        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Removes a season ticket item from the cart.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveSeason(int sectorId)
    {
        var cart = GetCart();
        cart.SeasonItems.RemoveAll(i => i.SectorId == sectorId);
        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Empties the entire cart including season items.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.Remove(CartSessionKey);
        return RedirectToAction(nameof(Index));
    }

    // ── Session helpers ────────────────────────────────────────────────────

    private CartVM GetCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json)) return new CartVM();
        return JsonSerializer.Deserialize<CartVM>(json) ?? new CartVM();
    }

    private void SaveCart(CartVM cart)
        => HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
}
