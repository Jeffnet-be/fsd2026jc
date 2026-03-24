using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace FullStackDevelopment_Ticketverkoop.Web.Controllers;

/// <summary>
/// Serves the introduction/home page and handles language switching.
/// The home page is available in NL, FR, and EN via ASP.NET Core Localization.
/// </summary>
public class HomeController : Controller
{
    private readonly IStringLocalizer<HomeController> _localizer;

    public HomeController(IStringLocalizer<HomeController> localizer)
    {
        _localizer = localizer;
    }

    public IActionResult Index() => View();

    /// <summary>
    /// Switches the UI language by setting a culture cookie and redirecting back.
    /// </summary>
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        Response.Cookies.Append(
            Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
            Microsoft.AspNetCore.Localization.CookieRequestCultureProvider
                .MakeCookieValue(new(culture)),
            new() { Expires = DateTimeOffset.UtcNow.AddYears(1) });

        return LocalRedirect(returnUrl);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}