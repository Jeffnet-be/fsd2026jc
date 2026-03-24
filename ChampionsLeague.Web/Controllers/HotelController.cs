using ChampionsLeague.Infrastructure.Services;
using ChampionsLeague.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChampionsLeague.Web.Controllers;

/// <summary>
/// Hotel search feature: calls the external IHotelApiService (typed HttpClient pattern).
/// Demonstrates consuming an external REST API as required by the project spec.
/// The city parameter can be pre-filled from the match detail page link.
/// </summary>
public class HotelController : Controller
{
    private readonly IHotelApiService _hotelApi;

    public HotelController(IHotelApiService hotelApi) => _hotelApi = hotelApi;

    /// <summary>GET /Hotel/Search — shows the search form, optionally pre-filled with a city.</summary>
    [HttpGet]
    public IActionResult Search(string? city = null)
        => View(new HotelSearchVM
        {
            City     = city ?? string.Empty,
            CheckIn  = DateTime.Today,
            CheckOut = DateTime.Today.AddDays(1)
        });

    /// <summary>
    /// POST /Hotel/Search — submits the search and renders results on the same page.
    /// ModelState.IsValid ensures City is not empty before calling the API.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(HotelSearchVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var apiResults = await _hotelApi.SearchAsync(model.City, model.CheckIn, model.CheckOut);

        model.Results = apiResults.Select(r => new HotelResultVM
        {
            Name          = r.Name,
            Address       = r.Address,
            PricePerNight = r.PricePerNight,
            BookingUrl    = r.BookingUrl
        });

        return View(model);
    }
}
