using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Handles culture/language switching.
/// Sets the ASP.NET Core culture cookie server-side (reliable on all platforms including Azure HTTPS)
/// and redirects back to the page the user was on.
/// </summary>
public class CultureController : Controller
{
    /// <summary>
    /// GET /Culture/Set?culture=nl&returnUrl=/Matches
    /// Sets the .AspNetCore.Culture cookie and redirects back to returnUrl.
    /// Server-side cookie setting is more reliable than JavaScript on Azure HTTPS.
    /// </summary>
    [HttpGet]
    public IActionResult Set(string culture, string returnUrl = "/")
    {
        // Validate culture to prevent open redirect or injection
        var supported = new[] { "nl", "fr", "en" };
        if (!supported.Contains(culture))
            culture = "nl";

        // Set the culture cookie using the official ASP.NET Core helper
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(culture)),
            new CookieOptions
            {
                Expires  = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = false,   // must be readable by browser for some frameworks
                IsEssential = true, // don't require consent banner
                SameSite = SameSiteMode.Lax,
                Secure = true
            }
        );

        // Redirect back to where the user was (safe local redirect only)
        if (!Url.IsLocalUrl(returnUrl))
            returnUrl = "/";

        return LocalRedirect(returnUrl);
    }
}
