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

            _logger.LogInformation(
                "Hotel search: {City} | {In:d} – {Out:d}", safeCity, checkIn, checkOut);

            var apiKey = _config["HotelApi:ApiKey"] ?? "";
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("HotelApi:ApiKey not configured");
                return Enumerable.Empty<HotelResult>();
            }

            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchDestination" +
                $"?query={Uri.EscapeDataString(safeCity)}");

            request.Headers.Add("x-rapidapi-key", apiKey);
            request.Headers.Add("x-rapidapi-host", "booking-com15.p.rapidapi.com");

            var response = await _http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<JsonElement>(json)
                                     .GetProperty("data")
                                     .EnumerateArray()
                                     .Take(4);

            return data.Select(item => new HotelResult(
                item.GetProperty("name").GetString() ?? "Hotel",
                item.GetProperty("city_name").GetString() ?? safeCity,
                0m,
                "https://www.booking.com"
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hotel API unavailable; returning empty list");
            return Enumerable.Empty<HotelResult>();
        }
    }
}
