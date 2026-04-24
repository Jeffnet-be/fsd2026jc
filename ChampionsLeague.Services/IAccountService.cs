using ChampionsLeague.Services.DTOs;

namespace ChampionsLeague.Services;

/// <summary>
/// Service-laag contract voor account-operaties.
///
/// ONTWERPKEUZE: SignInManager hoort NIET in de Services-laag.
/// SignInManager beheert authenticatie-cookies — dat is een HTTP/Web-concept.
/// Alleen UserManager (user-data: aanmaken, wachtwoord valideren, reset-tokens)
/// hoort hier thuis.
///
/// Login (cookie zetten) en Logout (cookie wissen) blijven in AccountController
/// via SignInManager — dat is correct want die kent de HTTP-context.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Registreert een nieuwe gebruiker.
    /// Retourneert fouten als e-mail al bestaat of wachtwoord niet voldoet.
    /// Logt de gebruiker NIET in — dat doet AccountController via SignInManager.
    /// </summary>
    Task<RegisterResultDto> RegisterAsync(RegisterDto dto);

    /// <summary>
    /// Controleert of e-mail en wachtwoord correct zijn.
    /// Retourneert de gevonden gebruiker zodat AccountController
    /// via SignInManager kan inloggen.
    /// Retourneert null als de combinatie ongeldig is.
    /// </summary>
    Task<UserDto?> ValidateCredentialsAsync(string email, string password);

    /// <summary>
    /// Genereert een wachtwoord-hersteltoken en stuurt de e-mail.
    /// De controller bouwt de callback-URL (heeft Url.Action nodig)
    /// en geeft die mee als parameter.
    /// </summary>
    Task SendPasswordResetEmailAsync(string email, string resetCallbackUrl);

    /// <summary>
    /// Stelt een nieuw wachtwoord in via een Identity-token.
    /// </summary>
    Task<RegisterResultDto> ResetPasswordAsync(string email, string token, string newPassword);
}
