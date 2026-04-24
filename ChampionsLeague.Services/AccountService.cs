using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Infrastructure.Services;
using ChampionsLeague.Services.DTOs;
using Microsoft.AspNetCore.Identity;

namespace ChampionsLeague.Services;

/// <summary>
/// Implementatie van <see cref="IAccountService"/>.
///
/// Gebruikt enkel UserManager — dat zit al in de bestaande
/// Microsoft.AspNetCore.Identity.EntityFrameworkCore package die dit project al heeft.
///
/// SignInManager is bewust WEGGELATEN: die beheert HTTP-cookies en hoort
/// in de Web-laag (AccountController). De scheiding is:
///   AccountService  → UserManager  (gebruikersdata: aanmaken, valideren, tokens)
///   AccountController → SignInManager (HTTP: cookie zetten/wissen)
/// </summary>
public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService                _email;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        IEmailService                email)
    {
        _userManager = userManager;
        _email       = email;
    }

    /// <summary>
    /// Registreert een nieuwe gebruiker via UserManager.
    /// Mapt Identity-foutcodes naar Nederlandstalige meldingen.
    /// Logt NIET in — dat doet de controller via SignInManager.
    /// </summary>
    public async Task<RegisterResultDto> RegisterAsync(RegisterDto dto)
    {
        var user = new ApplicationUser
        {
            UserName  = dto.Email,
            Email     = dto.Email,
            FirstName = dto.FirstName,
            LastName  = dto.LastName
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");

            // Geef de aangemaakte user terug zodat de controller direct kan inloggen
            return new RegisterResultDto { Success = true, CreatedUserId = user.Id };
        }

        var errors = result.Errors.Select(e => e.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail"
                => $"Er bestaat al een account met het e-mailadres '{dto.Email}'.",
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
            _ => e.Description
        }).ToList();

        return new RegisterResultDto { Success = false, Errors = errors };
    }

    /// <summary>
    /// Controleert of het wachtwoord klopt voor het opgegeven e-mailadres.
    /// Retourneert een UserDto als de combinatie geldig is, anders null.
    /// De controller gebruikt dit resultaat om via SignInManager in te loggen.
    /// </summary>
    public async Task<UserDto?> ValidateCredentialsAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return null;

        var isValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isValid) return null;

        return new UserDto
        {
            Id        = user.Id,
            Email     = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName  = user.LastName
        };
    }

    /// <summary>
    /// Genereert een wachtwoord-reset-token en stuurt de e-mail.
    /// Doet niets als het e-mailadres niet bestaat (anti-enumeration).
    /// De controller bouwt resetCallbackUrl via Url.Action — die URL-logica
    /// hoort niet in de service.
    /// </summary>
    public async Task SendPasswordResetEmailAsync(string email, string resetCallbackUrl)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return; // stil falen — geen user enumeration

        var token     = await _userManager.GeneratePasswordResetTokenAsync(user);
        var actualUrl = resetCallbackUrl.Replace("__TOKEN__", Uri.EscapeDataString(token));

        await _email.SendAsync(
            to      : email,
            subject : "Reset uw CL Tickets-wachtwoord",
            htmlBody: $@"<p>Hallo {user.FirstName},</p>
                         <p>Klik op de onderstaande link om uw wachtwoord te herstellen:</p>
                         <p><a href='{actualUrl}'>{actualUrl}</a></p>
                         <p>Deze link is 24 uur geldig.</p>"
        );
    }

    /// <summary>
    /// Stelt een nieuw wachtwoord in via een Identity reset-token.
    /// </summary>
    public async Task<RegisterResultDto> ResetPasswordAsync(
        string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return new RegisterResultDto { Success = true }; // stil falen

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (result.Succeeded)
            return new RegisterResultDto { Success = true };

        return new RegisterResultDto
        {
            Success = false,
            Errors  = result.Errors.Select(e => e.Description).ToList()
        };
    }
}
