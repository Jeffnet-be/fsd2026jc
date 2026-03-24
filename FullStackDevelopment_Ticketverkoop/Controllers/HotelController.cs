using FullStackDevelopment_Ticketverkoop.Services;
using Microsoft.AspNetCore.Mvc;

namespace FullStackDevelopment_Ticketverkoop.Web.Controllers;

/// <summary>
/// Allows users to search for hotel stays near the match city.
/// Results come from an external API — nothing is stored.
/// </summary>
public class HotelController : Controller
{
    private readonly IHotelService _hotelService;

    public HotelController(IHotelService hotelService) => _hotelService = hotelService;

    public IActionResult Index() => View();

    /// <summary>Calls the hotel API and returns results to the view.</summary>
    [HttpGet]
    public async Task<IActionResult> Search(string city, DateTime checkIn, DateTime checkOut)
    {
        if (string.IsNullOrWhiteSpace(city) || checkOut <= checkIn)
        {
            ModelState.AddModelError("", "Please enter a valid city and date range.");
            return View("Index");
        }

        var results = await _hotelService.SearchHotelsAsync(city, checkIn, checkOut);
        ViewBag.City = city;
        ViewBag.CheckIn = checkIn;
        ViewBag.CheckOut = checkOut;
        return View("Results", results);
    }
}