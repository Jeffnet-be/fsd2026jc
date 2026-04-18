using AutoMapper;
using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Services;
using ChampionsLeague.Services;
using ChampionsLeague.Web.Services;
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
    private readonly IMatchRepository              _matches;
    private readonly TranslationService            _tr;
    private readonly IMapper                       _mapper;
    private readonly ISeasonTicketRepository       _seasonTickets;


    public AccountController(
        UserManager<ApplicationUser>   userManager,
        SignInManager<ApplicationUser> signInManager,
        ITicketRepository              tickets,
        ITicketService                 ticketService,
        IEmailService                  email,
        IMatchRepository               matches,
        TranslationService             tr,
        IMapper                        mapper,
        ISeasonTicketRepository        seasonTickets)
    {
        _userManager   = userManager;
        _signInManager = signInManager;
        _tickets       = tickets;
        _ticketService = ticketService;
        _email         = email;
        _matches       = matches;
        _tr            = tr;
        _mapper        = mapper;
        _seasonTickets = seasonTickets;
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
        {
            // Identity uses "Username" internally (= email). Replace with a clear,
            // user-friendly message that never mentions the word "username".
            var message = error.Code switch
            {
                "DuplicateUserName" or "DuplicateEmail"
                    => $"An account with the email address '{model.Email}' already exists. " +
                        "Please log in or use a different email address.",
                "PasswordTooShort"
                    => "Password must be at least 12 characters.",
                "PasswordRequiresDigit"
                    => "Password must contain at least one digit (0–9).",
                "PasswordRequiresUpper"
                    => "Password must contain at least one uppercase letter (A–Z).",
                "PasswordRequiresLower"
                    => "Password must contain at least one lowercase letter (a–z).",
                "PasswordRequiresNonAlphanumeric"
                    => "Password must contain at least one special character (!@#$...).",
                _ => error.Description
            };
            ModelState.AddModelError(string.Empty, message);
        }

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
        var userId = _userManager.GetUserId(User)!;
        var tickets = await _tickets.GetUserTicketHistoryAsync(userId);
        var seasonTickets = await _seasonTickets.GetUserSeasonTicketsAsync(userId);

        var vms = _mapper.Map<IEnumerable<TicketHistoryItemVM>>(tickets);

        var seasonVms = seasonTickets.Select(st => new SeasonTicketHistoryVM
        {
            Id = st.Id,
            SectorName = st.Sector?.Name ?? "",
            SeatNumber = st.SeatNumber,
            TotalPrice = st.TotalPrice,
            PurchasedAt = st.PurchasedAt,
            IsActive = st.IsActive
        });

        ViewBag.SeasonTickets = seasonVms;
        return View(vms);
    }

    // ── Cancel ticket ─────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> CancelTicket(int ticketId)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _ticketService.CancelAsync(ticketId, userId);

        if (result.Success)
            TempData["Success"] = _tr.T("tickets_cancel_success");
        else
            TempData["Error"] = result.ErrorMessage;

        return RedirectToAction(nameof(MyTickets));
    }

    // ── Resend voucher email ──────────────────────────────────────────

    /// <summary>
    /// POST /Account/ResendVoucher — resends the voucher email for a single ticket.
    /// Only allowed for the ticket owner, and only for Paid (non-cancelled) tickets.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> ResendVoucher(int ticketId)
    {
        var userId  = _userManager.GetUserId(User)!;
        var user    = await _userManager.FindByIdAsync(userId);
        var tickets = await _tickets.GetUserTicketsAsync(userId);
        var ticket  = tickets.FirstOrDefault(t => t.Id == ticketId);

        if (ticket is null || user is null)
        {
            TempData["Error"] = _tr.T("voucher_resend_error");
            return RedirectToAction(nameof(MyTickets));
        }

        if (ticket.Status == TicketStatus.Cancelled)
        {
            TempData["Error"] = _tr.T("voucher_resend_cancelled");
            return RedirectToAction(nameof(MyTickets));
        }

        var allMatches  = await _matches.GetAllWithClubsAsync();
        var match       = allMatches.FirstOrDefault(m => m.Id == ticket.MatchId);
        var matchDesc   = match is not null
            ? $"{match.HomeClub?.Name} vs {match.AwayClub?.Name}"
            : "Unknown match";
        var matchDate   = match?.MatchDate.ToString("dd MMMM yyyy") ?? "";

        await _email.SendAsync(
            to      : user.Email!,
            subject : $"{_tr.T("voucher_resend_subject")} — {matchDesc}",
            htmlBody: $@"
<p>Hello {user.FirstName},</p>
<p>{_tr.T("voucher_resend_intro")} <strong>{matchDesc}</strong> ({matchDate}):</p>
<table style='border-collapse:collapse;font-family:Arial,sans-serif'>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>{_tr.T("tickets_col_sector")}:</td>
      <td style='padding:6px 0;font-weight:bold'>{ticket.Sector?.Name ?? ""}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>{_tr.T("tickets_col_seat")}:</td>
      <td style='padding:6px 0;font-weight:bold'>{ticket.SeatNumber}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>{_tr.T("tickets_col_voucher")}:</td>
      <td style='padding:6px 0;font-family:monospace;font-size:14px;font-weight:bold;color:#001489'>{ticket.VoucherId:D}</td></tr>
</table>
<p>{_tr.T("voucher_resend_footer")}</p>
<p>CL Tickets Portal</p>"
        );

        TempData["Success"] = _tr.T("voucher_resend_success");
        return RedirectToAction(nameof(MyTickets));
    }
}
