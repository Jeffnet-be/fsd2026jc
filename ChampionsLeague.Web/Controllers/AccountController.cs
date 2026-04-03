using AutoMapper;
using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Services;
using ChampionsLeague.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Handles registration, login, logout, forgot/reset password,
/// ticket history, and ticket cancellation.
/// Uses ASP.NET Core Identity's UserManager and SignInManager.
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser>   _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITicketRepository             _tickets;
    private readonly ITicketService                _ticketService;
    private readonly IEmailService                 _email;
    private readonly IMapper                       _mapper;

    public AccountController(
        UserManager<ApplicationUser>   userManager,
        SignInManager<ApplicationUser> signInManager,
        ITicketRepository              tickets,
        ITicketService                 ticketService,
        IEmailService                  email,
        IMapper                        mapper)
    {
        _userManager   = userManager;
        _signInManager = signInManager;
        _tickets       = tickets;
        _ticketService = ticketService;
        _email         = email;
        _mapper        = mapper;
    }

    // ── Registration ──────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Register() => View(new RegisterVM());

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

    // ── Forgot Password ───────────────────────────────────────────────────

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordVM());

    /// <summary>
    /// Generates a password-reset token via Identity and sends it by email.
    /// Always shows the same confirmation (no user enumeration).
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null)
        {
            var token       = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink   = Url.Action("ResetPassword", "Account",
                                  new { token, email = model.Email },
                                  Request.Scheme)!;

            await _email.SendAsync(
                to      : model.Email,
                subject : "Reset your CL Tickets password",
                htmlBody: $@"<p>Hello {user.FirstName},</p>
                             <p>Click the link below to reset your password:</p>
                             <p><a href='{resetLink}'>{resetLink}</a></p>
                             <p>This link is valid for 24 hours.</p>"
            );
        }

        // Always redirect to confirmation — prevents user enumeration
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    // ── Reset Password ────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult ResetPassword(string? token = null, string? email = null)
    {
        if (token == null || email == null)
            return BadRequest("Invalid password reset link.");

        return View(new ResetPasswordVM { Token = token, Email = email });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null)
        {
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }
        }

        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation() => View();

    // ── Ticket history ────────────────────────────────────────────────────

    [Authorize]
    public async Task<IActionResult> MyTickets()
    {
        var userId  = _userManager.GetUserId(User)!;
        var tickets = await _tickets.GetUserTicketHistoryAsync(userId); // full history incl. cancelled
        var vms     = _mapper.Map<IEnumerable<TicketHistoryItemVM>>(tickets);
        return View(vms);
    }

    // ── Cancel ticket ─────────────────────────────────────────────────────

    /// <summary>
    /// POST /Account/CancelTicket — cancels a single ticket.
    /// Free cancellation enforced up to 7 days before match in TicketService.CancelAsync.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> CancelTicket(int ticketId)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _ticketService.CancelAsync(ticketId, userId);

        if (result.Success)
            TempData["Success"] = "Ticket cancelled successfully.";
        else
            TempData["Error"] = result.ErrorMessage;

        return RedirectToAction(nameof(MyTickets));
    }
}
