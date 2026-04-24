using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Beheert de sessie-gebaseerde winkelwagen.
///
/// BUG FIX 1 — camelCase JSON deserialisatie:
/// System.Text.Json is standaard case-sensitive. De JavaScript stuurt
/// { matchId, sectorId } (camelCase) maar CartItemVM heeft MatchId en SectorId
/// (PascalCase). Zonder JsonSerializerOptions.PropertyNameCaseInsensitive
/// bleven die velden 0, waardoor Remove() nooit het juiste item vond.
///
/// BUG FIX 2 — consistente serialisatie-opties:
/// GetCart() en SaveCart() gebruiken nu dezelfde opties zodat wat er in
/// de sessie staat altijd correct teruggelezen wordt.
/// </summary>
public class CartController : Controller
{
    private const string CartSessionKey = "CART";

    // Gedeelde opties: case-insensitief lezen én schrijven in camelCase
    // zodat de JSON in de sessie consistent is.
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,          // lost de AJAX camelCase bug op
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase
    };

    /// <summary>Toont de volledige winkelwagen-pagina.</summary>
    public IActionResult Index()
    {
        var cart = GetCart();
        return View(cart);
    }

    /// <summary>
    /// Voegt één item toe via AJAX (POST + JSON body).
    /// Geeft een PartialView terug zodat de badge bijgewerkt wordt zonder full reload.
    ///
    /// FIX: [FromBody] gebruikt nu de gedeelde _jsonOptions met
    /// PropertyNameCaseInsensitive = true. Hierdoor worden { matchId, sectorId }
    /// van de JavaScript correct gemapt op MatchId en SectorId van CartItemVM.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddToCart([FromBody] CartItemVM item)
    {
        if (item is null)
            return BadRequest(new { error = "Invalid cart item." });

        var cart = GetCart();

        var alreadyInCart = cart.Items
            .Where(i => i.MatchId == item.MatchId)
            .Sum(i => i.Quantity);

        if (alreadyInCart + item.Quantity > 4)
            return BadRequest(new { error = "Maximum 4 tickets per match allowed." });

        var existing = cart.Items
            .FirstOrDefault(i => i.MatchId == item.MatchId && i.SectorId == item.SectorId);

        if (existing is not null)
            existing.Quantity += item.Quantity;
        else
            cart.Items.Add(item);

        SaveCart(cart);
        return PartialView("_CartSummary", cart);
    }

    /// <summary>
    /// Verwijdert één matchId+sectorId-regel.
    /// Werkt correct zolang de items in de sessie echte MatchId-waarden hebben
    /// (gegarandeerd door de camelCase-fix in AddToCart).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int matchId, int sectorId)
    {
        var cart = GetCart();
        cart.Items.RemoveAll(i => i.MatchId == matchId && i.SectorId == sectorId);
        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Verwijdert één seizoensabonnement-regel op basis van sectorId.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveSeason(int sectorId)
    {
        var cart = GetCart();
        cart.SeasonItems.RemoveAll(i => i.SectorId == sectorId);
        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Leegt de volledige winkelwagen.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.Remove(CartSessionKey);
        return RedirectToAction(nameof(Index));
    }

    // ── Session helpers ───────────────────────────────────────────────────

    private CartVM GetCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json)) return new CartVM();
        return JsonSerializer.Deserialize<CartVM>(json, _jsonOptions) ?? new CartVM();
    }

    private void SaveCart(CartVM cart)
        => HttpContext.Session.SetString(CartSessionKey,
               JsonSerializer.Serialize(cart, _jsonOptions));
}
