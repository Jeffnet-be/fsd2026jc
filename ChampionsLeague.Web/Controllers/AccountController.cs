using AutoMapper;
using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Infrastructure.Services;
using ChampionsLeague.Services;
using ChampionsLeague.Web.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Registratie, login, uitloggen, wachtwoord vergeten/herstellen,
/// ticketgeschiedenis en annulaties.
///
/// REFACTOR: ITicketRepository, ISeasonTicketRepository en IMatchRepository
/// zijn verwijderd. In de plaats:
/// - IUserTicketService  → historiek, actieve tickets, voucher-herversturing
/// - ISeasonTicketService → abonnementen-historiek en annulatie
/// - ITicketService      → annulatie losse tickets (was al aanwezig)
///
/// De controller bevat nu GEEN directe repository-aanroepen meer.
///
/// UITLOGGEN + WINKELWAGEN:
/// Bij uitloggen wordt de sessie gewist via SignOutAsync(), wat de hele sessie
/// (inclusief winkelwagen) verwijdert. Dit lost het "wagen blijft na uitloggen"-probleem op.
/// Expliciet Session.Remove(CartSessionKey) is redundant maar verduidelijkend toegevoegd.
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser>   _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITicketService                 _ticketService;
    private readonly IUserTicketService             _userTicketService;
    private readonly ISeasonTicketService           _seasonTicketService;
    private readonly IEmailService                  _email;
    private readonly TranslationService             _tr;
    private readonly IMapper                        _mapper;

    private const string CartSessionKey = "CART";

    public AccountController(
        UserManager<ApplicationUser>   userManager,
        SignInManager<ApplicationUser> signInManager,
        ITicketService                 ticketService,
        IUserTicketService             userTicketService,
        ISeasonTicketService           seasonTicketService,
        IEmailService                  email,
        TranslationService             tr,
        IMapper                        mapper)
    {
        _userManager         = userManager;
        _signInManager       = signInManager;
        _ticketService       = ticketService;
        _userTicketService   = userTicketService;
        _seasonTicketService = seasonTicketService;
        _email               = email;
        _tr                  = tr;
        _mapper              = mapper;
    }

    // ── Registratie ───────────────────────────────────────────────────────

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
            var message = error.Code switch
            {
                "DuplicateUserName" or "DuplicateEmail"
                    => $"Er bestaat al een account met het e-mailadres '{model.Email}'.",
                "PasswordTooShort"
                    => "Wachtwoord moet minstens 12 tekens bevatten.",
                "PasswordRequiresDigit"
                    => "Wachtwoord moet minstens één cijfer bevatten (0–9).",
                "PasswordRequiresUpper"
                    => "Wachtwoord moet minstens één hoofdletter bevatten (A–Z).",
                "PasswordRequiresLower"
                    => "Wachtwoord moet minstens één kleine letter bevatten (a–z).",
                "PasswordRequiresNonAlphanumeric"
                    => "Wachtwoord moet minstens één speciaal teken bevatten (!@#$...).",
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

        ModelState.AddModelError(string.Empty, "Ongeldig e-mailadres of wachtwoord.");
        return View(model);
    }

    // ── Uitloggen ─────────────────────────────────────────────────────────

    /// <summary>
    /// Logt de gebruiker uit en wist de sessie (inclusief winkelwagen).
    ///
    /// BUG FIX UITLOGGEN + WINKELWAGEN:
    /// SignOutAsync() roept intern HttpContext.Session.Clear() aan als de sessie
    /// gekoppeld is aan de authenticatie-cookie. Om absoluut zeker te zijn dat
    /// de wagen verdwijnt, wissen we hem hier expliciet vóór het uitloggen.
    /// Zonder deze regel bleef de sessie-data soms staan bij bepaalde configuraties.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Winkelwagen expliciet wissen vóór uitloggen
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

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null)
        {
            var token     = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account",
                                new { token, email = model.Email }, Request.Scheme)!;

            await _email.SendAsync(
                to      : model.Email,
                subject : "Reset uw CL Tickets-wachtwoord",
                htmlBody: $@"<p>Hallo {user.FirstName},</p>
                             <p>Klik op de onderstaande link om uw wachtwoord te herstellen:</p>
                             <p><a href='{resetLink}'>{resetLink}</a></p>
                             <p>Deze link is 24 uur geldig.</p>"
            );
        }

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

    // ── Ticketgeschiedenis ────────────────────────────────────────────────

    /// <summary>
    /// Toont de volledige ticket- en abonnementsgeschiedenis van de gebruiker.
    ///
    /// REFACTOR: vroeger riep dit rechtstreeks _tickets.GetUserTicketHistoryAsync()
    /// en _seasonTickets.GetAllUserSeasonTicketsAsync() aan.
    /// Nu gebruikt het IUserTicketService en ISeasonTicketService — geen repository-calls.
    /// </summary>
    [Authorize]
    public async Task<IActionResult> MyTickets()
    {
        var userId = _userManager.GetUserId(User)!;

        var tickets       = await _userTicketService.GetHistoryAsync(userId);
        var seasonTickets = await _seasonTicketService.GetAllUserSeasonTicketsAsync(userId);

        var vms = _mapper.Map<IEnumerable<TicketHistoryItemVM>>(tickets);

        var seasonVms = seasonTickets.Select(st => new SeasonTicketHistoryVM
        {
            Id          = st.Id,
            ClubName    = st.Sector?.Stadium?.Club?.Name    ?? "",
            StadiumName = st.Sector?.Stadium?.Name          ?? "",
            SectorName  = st.Sector?.Name                   ?? "",
            SeatNumber  = st.SeatNumber,
            TotalPrice  = st.TotalPrice,
            PurchasedAt = st.PurchasedAt,
            IsActive    = st.IsActive,
            VoucherId   = st.VoucherId
        });

        ViewBag.SeasonTickets = seasonVms;
        return View(vms);
    }

    // ── Annulatie los ticket ──────────────────────────────────────────────

    /// <summary>
    /// Annuleert een los ticket via ITicketService.
    /// Na annulatie (Status = Cancelled) wordt het stoelnummer terug vrijgegeven:
    /// GetReservedSeatsAsync filtert op Status != Cancelled, dus de stoel is automatisch vrij.
    ///
    /// BUG FIX ANNULATIE: De eerder gemelde bug (ticket niet terug vrij na annulatie)
    /// is opgelost in TicketRepository.GetReservedSeatsAsync(), dat al filtert op
    /// t.Status != TicketStatus.Cancelled. De service zet Status correct op Cancelled.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> CancelTicket(int ticketId)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _ticketService.CancelAsync(ticketId, userId);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success ? _tr.T("tickets_cancel_success") : result.ErrorMessage;

        return RedirectToAction(nameof(MyTickets));
    }

    // ── Voucher herversturing ─────────────────────────────────────────────

    /// <summary>
    /// Stuurt de voucher-e-mail opnieuw voor een los ticket.
    /// De logica (eigenaarschapscontrole, e-mail) zit nu in IUserTicketService.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> ResendVoucher(int ticketId)
    {
        var userId     = _userManager.GetUserId(User)!;
        var user       = await _userManager.FindByIdAsync(userId);
        var lang       = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        var (success, error) = await _userTicketService.ResendVoucherAsync(
            ticketId, userId, user?.Email ?? "", user?.FirstName ?? "", lang);

        TempData[success ? "Success" : "Error"] =
            success ? _tr.T("voucher_resend_success") : error;

        return RedirectToAction(nameof(MyTickets));
    }

    // ── Annulatie abonnement ──────────────────────────────────────────────

    /// <summary>
    /// Annuleert een seizoensabonnement via ISeasonTicketService.
    ///
    /// BUG FIX: Vroeger deed de controller dit rechtstreeks via de repository.
    /// Nu zit de businesslogica (eigenaarschapscontrole, IsActive = false) in de service.
    /// Na annulatie (IsActive = false) filtert GetSeasonReservedSeatsAsync dat stoelnummer
    /// niet meer mee → het is automatisch terug beschikbaar.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> CancelSeasonTicket(int seasonTicketId)
    {
        var userId          = _userManager.GetUserId(User)!;
        var (success, error) = await _seasonTicketService.CancelAsync(seasonTicketId, userId);

        TempData[success ? "Success" : "Error"] =
            success ? "Abonnement geannuleerd." : error;

        return RedirectToAction(nameof(MyTickets));
    }

    // ── Voucher herversturing abonnement ──────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> ResendSeasonVoucher(int seasonTicketId)
    {
        var userId = _userManager.GetUserId(User)!;
        var user   = await _userManager.FindByIdAsync(userId);

        var tickets = await _seasonTicketService.GetUserSeasonTicketsAsync(userId);
        var ticket  = tickets.FirstOrDefault(t => t.Id == seasonTicketId);

        if (ticket is null || user is null)
        {
            TempData["Error"] = "Abonnement niet gevonden.";
            return RedirectToAction(nameof(MyTickets));
        }

        await _email.SendAsync(
            to      : user.Email!,
            subject : $"Uw abonnement — {ticket.Sector?.Stadium?.Club?.Name}",
            htmlBody: $@"
<p>Hallo {user.FirstName},</p>
<p>Hieronder vindt u de gegevens van uw seizoensabonnement:</p>
<table style='border-collapse:collapse;font-family:Arial,sans-serif'>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>Club:</td>
      <td style='padding:6px 0;font-weight:bold'>{ticket.Sector?.Stadium?.Club?.Name}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>Stadion:</td>
      <td style='padding:6px 0;font-weight:bold'>{ticket.Sector?.Stadium?.Name}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>Vak:</td>
      <td style='padding:6px 0;font-weight:bold'>{ticket.Sector?.Name}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>Zitplaats:</td>
      <td style='padding:6px 0;font-weight:bold'>{ticket.SeatNumber}</td></tr>
  <tr><td style='padding:6px 16px 6px 0;color:#666'>Totaalprijs:</td>
      <td style='padding:6px 0;font-weight:bold'>€ {ticket.TotalPrice:0.00}</td></tr>
</table>
<p>Dit abonnement is geldig voor alle thuiswedstrijden van het seizoen.</p>
<p>CL Tickets Portal</p>"
        );

        TempData["Success"] = "Voucher opnieuw verzonden.";
        return RedirectToAction(nameof(MyTickets));
    }
}
