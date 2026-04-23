using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ChampionsLeague.Infrastructure.Services;

/// <summary>Contract for the external hotel-search API.</summary>
public interface IHotelApiService
{
    /// <summary>Returns a list of available hotels in the given city for the given dates.</summary>
    Task<IEnumerable<HotelResult>> SearchAsync(string city, DateTime checkIn, DateTime checkOut);
}

/// <summary>
/// DTO returned to callers — only the fields we display in the hotel results view.
/// Using a record keeps it immutable and concise.
/// </summary>
public record HotelResult(string Name, string Address, decimal PricePerNight, string BookingUrl);

/// <summary>
/// Typed HttpClient implementation for the hotel-search REST API.
/// HttpClient is injected via the typed-client pattern registered in Program.cs.
/// </summary>
public class HotelApiService : IHotelApiService
{
    private readonly HttpClient             _http;
    private readonly ILogger<HotelApiService> _logger;
    private readonly IConfiguration _config;

    public HotelApiService(HttpClient http, ILogger<HotelApiService> logger, IConfiguration config)
    {
        _http = http;
        _logger = logger;
        _config = config;
    }
    public async Task<IEnumerable<HotelResult>> SearchAsync(
    string city, DateTime checkIn, DateTime checkOut)
    {
        try
        {
            var safeCity = city?.Replace("\n", "").Replace("\r", "").Replace("\t", "") ?? "";
            var apiKey = _config["HotelApi:ApiKey"] ?? "";

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("HotelApi:ApiKey not configured");
                return Enumerable.Empty<HotelResult>();
            }

            _logger.LogInformation("Hotel search: {City} | {In:d} – {Out:d}", safeCity, checkIn, checkOut);

            // ── Step 1: get dest_id from city name ────────────────────────
            using var destRequest = new HttpRequestMessage(HttpMethod.Get,
                $"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchDestination" +
                $"?query={Uri.EscapeDataString(safeCity)}");
            destRequest.Headers.Add("x-rapidapi-key", apiKey);
            destRequest.Headers.Add("x-rapidapi-host", "booking-com15.p.rapidapi.com");

            var destResponse = await _http.SendAsync(destRequest);
            var destJson = await destResponse.Content.ReadAsStringAsync();
            var destData = JsonSerializer.Deserialize<JsonElement>(destJson);

            // Find the first result with search_type = "city"
            var destId = destData
                .GetProperty("data")
                .EnumerateArray()
                .FirstOrDefault(e => e.TryGetProperty("search_type", out var t)
                                  && t.GetString() == "city")
                .GetProperty("dest_id")
                .GetString();

            if (string.IsNullOrEmpty(destId))
                return Enumerable.Empty<HotelResult>();

            // ── Step 2: search actual hotels with that dest_id ────────────
            using var hotelRequest = new HttpRequestMessage(HttpMethod.Get,
                $"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchHotels" +
                $"?dest_id={Uri.EscapeDataString(destId)}" +
                $"&search_type=CITY" +
                $"&arrival_date={checkIn:yyyy-MM-dd}" +
                $"&departure_date={checkOut:yyyy-MM-dd}" +
                $"&adults=1&room_qty=1&currency_code=EUR&languagecode=nl");
            hotelRequest.Headers.Add("x-rapidapi-key", apiKey);
            hotelRequest.Headers.Add("x-rapidapi-host", "booking-com15.p.rapidapi.com");

            var hotelResponse = await _http.SendAsync(hotelRequest);
            var hotelJson = await hotelResponse.Content.ReadAsStringAsync();
            var hotelData = JsonSerializer.Deserialize<JsonElement>(hotelJson);

            return hotelData
                .GetProperty("data")
                .GetProperty("hotels")
                .EnumerateArray()
                .Take(4)
                .Select(hotel =>
                {
                    var prop = hotel.GetProperty("property");

                    var name = prop.TryGetProperty("name", out var n)
                                  ? n.GetString() ?? "Hotel" : "Hotel";
                    var address = prop.TryGetProperty("wishlistName", out var a)
                                  ? a.GetString() ?? safeCity : safeCity;

                    decimal price = 0;
                    if (prop.TryGetProperty("priceBreakdown", out var pb) &&
                        pb.TryGetProperty("grossPrice", out var gp) &&
                        gp.TryGetProperty("value", out var pv))
                        price = pv.GetDecimal();

                    return new HotelResult(name, address, price, "https://www.booking.com");
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hotel API unavailable; returning empty list");
            return Enumerable.Empty<HotelResult>();
        }
    }
}
