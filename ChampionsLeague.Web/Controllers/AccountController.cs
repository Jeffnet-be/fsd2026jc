using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Infrastructure.Services;
using ChampionsLeague.Services;
using ChampionsLeague.Services.DTOs;
using ChampionsLeague.Web.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Registratie, login, uitloggen, wachtwoord vergeten/herstellen,
/// ticketgeschiedenis en annulaties.
///
/// SignInManager blijft hier in de Web-laag — dat is correct.
/// SignInManager beheert authenticatie-cookies (HTTP-concept) en hoort
/// niet in de Services-laag thuis.
///
/// AccountService doet het Identity-werk dat wel in Services past:
///   - Gebruiker aanmaken (UserManager.CreateAsync)
///   - Wachtwoord valideren (UserManager.CheckPasswordAsync)
///   - Reset-tokens genereren en e-mails sturen
///
/// AccountController doet het HTTP-werk:
///   - Cookie zetten na succesvolle login (SignInManager.SignInAsync)
///   - Cookie wissen bij uitloggen (SignInManager.SignOutAsync)
///   - Winkelwagen wissen bij uitloggen (Session.Remove)
/// </summary>
public class AccountController : Controller
{
    private readonly IAccountService               _accountService;
    private readonly ITicketService                _ticketService;
    private readonly IUserTicketService            _userTicketService;
    private readonly ISeasonTicketService          _seasonTicketService;
    private readonly IEmailService                 _email;
    private readonly TranslationService            _tr;
    // SignInManager en UserManager blijven in de Web-laag
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser>   _userManager;

    private const string CartSessionKey = "CART";

    public AccountController(
        IAccountService                accountService,
        ITicketService                 ticketService,
        IUserTicketService             userTicketService,
        ISeasonTicketService           seasonTicketService,
        IEmailService                  email,
        TranslationService             tr,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser>   userManager)
    {
        _accountService      = accountService;
        _ticketService       = ticketService;
        _userTicketService   = userTicketService;
        _seasonTicketService = seasonTicketService;
        _email               = email;
        _tr                  = tr;
        _signInManager       = signInManager;
        _userManager         = userManager;
    }

    // ── Registratie ───────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Register() => View(new RegisterVM());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVM model)
    {
        if (!ModelState.IsValid) return View(model);

        // AccountService maakt de gebruiker aan (UserManager — Services-laag)
        var result = await _accountService.RegisterAsync(new RegisterDto
        {
            FirstName = model.FirstName,
            LastName  = model.LastName,
            Email     = model.Email,
            Password  = model.Password
        });

        if (result.Success)
        {
            // SignInManager blijft hier: cookie zetten is een Web/HTTP-operatie
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is not null)
                await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "Home");
        }

        foreach (var err in result.Errors)
            ModelState.AddModelError(string.Empty, err);

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

        // AccountService valideert credentials (UserManager — Services-laag)
        var userDto = await _accountService.ValidateCredentialsAsync(model.Email, model.Password);

        if (userDto is null)
        {
            ModelState.AddModelError(string.Empty, "Ongeldig e-mailadres of wachtwoord.");
            return View(model);
        }

        // SignInManager zet de cookie (Web-laag — correct)
        var user = await _userManager.FindByIdAsync(userDto.Id);
        if (user is not null)
            await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);

        return LocalRedirect(returnUrl ?? "/");
    }

    // ── Uitloggen ─────────────────────────────────────────────────────────

    /// <summary>
    /// BUG FIX: winkelwagen wissen vóór uitloggen.
    /// Session.Remove werkt zolang de sessie nog actief is —
    /// dus vóór SignOutAsync aanroepen.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Remove(CartSessionKey);
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // ── Wachtwoord vergeten ───────────────────────────────────────────────

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordVM());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
    {
        if (!ModelState.IsValid) return View(model);

        // Controller bouwt de URL (heeft toegang tot Url.Action)
        // Service verstuurt de e-mail met het gegenereerde token
        var resetLink = Url.Action("ResetPassword", "Account",
            new { email = model.Email, token = "__TOKEN__" }, Request.Scheme)!;

        await _accountService.SendPasswordResetEmailAsync(model.Email, resetLink);

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    // ── Wachtwoord herstellen ─────────────────────────────────────────────

    [HttpGet]
    public IActionResult ResetPassword(string? token = null, string? email = null)
    {
        if (token == null || email == null)
            return BadRequest("Ongeldige wachtwoord-herstelkoppeling.");
        return View(new ResetPasswordVM { Token = token, Email = email });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _accountService.ResetPasswordAsync(
            model.Email, model.Token, model.Password);

        if (!result.Success)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err);
            return View(model);
        }

        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation() => View();

    // ── Ticketgeschiedenis ────────────────────────────────────────────────

    [Authorize]
    public async Task<IActionResult> MyTickets()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var ticketDtos       = await _userTicketService.GetHistoryAsync(userId);
        var seasonTicketDtos = await _seasonTicketService.GetAllUserSeasonTicketsAsync(userId);

        var ticketVms = ticketDtos.Select(d => new TicketHistoryItemVM
        {
            Id               = d.Id,
            MatchDescription = d.MatchDescription,
            MatchDate        = d.MatchDate,
            SectorName       = d.SectorName,
            SeatNumber       = d.SeatNumber,
            PricePaid        = d.PricePaid,
            VoucherId        = d.VoucherId,
            Status           = d.Status,
            IsCancellable    = d.IsCancellable
        });

        var seasonVms = seasonTicketDtos.Select(d => new SeasonTicketHistoryVM
        {
            Id          = d.Id,
            ClubName    = d.ClubName,
            StadiumName = d.StadiumName,
            SectorName  = d.SectorName,
            SeatNumber  = d.SeatNumber,
            TotalPrice  = d.TotalPrice,
            PurchasedAt = d.PurchasedAt,
            IsActive    = d.IsActive,
            VoucherId   = d.VoucherId
        });

        ViewBag.SeasonTickets = seasonVms;
        return View(ticketVms);
    }

    // ── Annulatie los ticket ──────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> CancelTicket(int ticketId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _ticketService.CancelAsync(ticketId, userId);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success ? _tr.T("tickets_cancel_success") : result.ErrorMessage;

        return RedirectToAction(nameof(MyTickets));
    }

    // ── Voucher herversturing los ticket ──────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> ResendVoucher(int ticketId)
    {
        var userId    = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userEmail = User.FindFirstValue(ClaimTypes.Email)         ?? "";
        var firstName = User.FindFirstValue(ClaimTypes.GivenName)     ?? "";
        var lang      = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        var (success, error) = await _userTicketService.ResendVoucherAsync(
            ticketId, userId, userEmail, firstName, lang);

        TempData[success ? "Success" : "Error"] =
            success ? _tr.T("voucher_resend_success") : error;

        return RedirectToAction(nameof(MyTickets));
    }

    // ── Annulatie abonnement ──────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> CancelSeasonTicket(int seasonTicketId)
    {
        var userId            = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, error)  = await _seasonTicketService.CancelAsync(seasonTicketId, userId);

        TempData[success ? "Success" : "Error"] =
            success ? "Abonnement geannuleerd." : error;

        return RedirectToAction(nameof(MyTickets));
    }

    // ── Voucher herversturing abonnement ──────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> ResendSeasonVoucher(int seasonTicketId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var dtos   = await _seasonTicketService.GetUserSeasonTicketsAsync(userId);
        var ticket = dtos.FirstOrDefault(t => t.Id == seasonTicketId);

        if (ticket is null)
        {
            TempData["Error"] = "Abonnement niet gevonden.";
            return RedirectToAction(nameof(MyTickets));
        }

        var userEmail = User.FindFirstValue(ClaimTypes.Email)     ?? "";
        var firstName = User.FindFirstValue(ClaimTypes.GivenName) ?? "";

        await _email.SendAsync(
            to      : userEmail,
            subject : $"Uw abonnement — {ticket.ClubName}",
            htmlBody: $@"
<p>Hallo {firstName},</p>
<p>Hieronder vindt u de gegevens van uw seizoensabonnement:</p>
<table style='border-collapse:collapse;font-family:Arial,sans-serif'>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>Club:</td>
      <td style='font-weight:bold'>{ticket.ClubName}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>Stadion:</td>
      <td style='font-weight:bold'>{ticket.StadiumName}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>Vak:</td>
      <td style='font-weight:bold'>{ticket.SectorName}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>Zitplaats:</td>
      <td style='font-weight:bold'>{ticket.SeatNumber}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>Totaalprijs:</td>
      <td style='font-weight:bold'>€ {ticket.TotalPrice:0.00}</td></tr>
</table>
<p>CL Tickets Portal</p>"
        );

        TempData["Success"] = "Voucher opnieuw verzonden.";
        return RedirectToAction(nameof(MyTickets));
    }
}
