using AutoMapper;
using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Handles registration, login, logout, and the personal ticket history.
/// Uses ASP.NET Core Identity's UserManager and SignInManager — no custom
/// password hashing or session management is written by hand.
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser>   _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITicketRepository             _tickets;
    private readonly IMapper                       _mapper;

    public AccountController(
        UserManager<ApplicationUser>   userManager,
        SignInManager<ApplicationUser> signInManager,
        ITicketRepository              tickets,
        IMapper                        mapper)
    {
        _userManager   = userManager;
        _signInManager = signInManager;
        _tickets       = tickets;
        _mapper        = mapper;
    }

    // ── Registration ──────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Register() => View(new RegisterVM());

    /// <summary>
    /// POST /Account/Register — creates a new ApplicationUser via Identity.
    /// ModelState.IsValid checks all Data Annotation rules on RegisterVM
    /// (Required, EmailAddress, StringLength, Compare — curriculum section 8.1).
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName  = model.Email,
            Email     = model.Email,
            FirstName = model.FirstName,
            LastName  = model.LastName
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        // Map Identity errors back to ModelState so they appear in asp-validation-summary
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // ── Login ─────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginVM());
    }

    /// <summary>
    /// POST /Account/Login — validates credentials via SignInManager.
    /// PasswordSignInAsync handles password hashing comparison internally.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? "/");

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    // ── Logout ────────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // ── Ticket history ────────────────────────────────────────────────────

    /// <summary>
    /// GET /Account/MyTickets — returns the authenticated user's ticket history.
    /// AutoMapper projects Ticket → TicketHistoryItemVM (includes match description,
    /// cancellability flag, voucher GUID).
    /// </summary>
    [Authorize]
    public async Task<IActionResult> MyTickets()
    {
        var userId  = _userManager.GetUserId(User)!;
        var tickets = await _tickets.GetUserTicketsAsync(userId);
        var vms     = _mapper.Map<IEnumerable<TicketHistoryItemVM>>(tickets);
        return View(vms);
    }
}
