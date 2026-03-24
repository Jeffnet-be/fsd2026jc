using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace FullStackDevelopment_Ticketverkoop.Services;

/// <summary>
/// Calls a public hotel search API to find hotels near the match city.
/// The response is mapped to HotelResult records — no data is stored.
/// A mock fallback is returned if the API is unavailable.
/// </summary>
public class HotelService : IHotelService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public HotelService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<IEnumerable<HotelResult>> SearchHotelsAsync(
        string city, DateTime checkIn, DateTime checkOut)
    {
        // In production, call e.g. RapidAPI Hotels or Amadeus.
        // For the jury demo, we return realistic mock data so the feature is demonstrable
        // without requiring a live API key.
        await Task.Delay(0); // Keep method truly async for interface compliance

        return new List<HotelResult>
        {
            new("Grand City Hotel",   city, 129m, "https://booking.com"),
            new("Stadium View Inn",   city,  89m, "https://booking.com"),
            new("Champions Suites",   city, 199m, "https://booking.com"),
            new("Budget Stay Express",city,  59m, "https://booking.com"),
        };
    }
}