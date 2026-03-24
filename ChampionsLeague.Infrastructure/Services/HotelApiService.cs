using Microsoft.Extensions.Logging;

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
/// If the real API is unavailable, returns mock data so the demo still works.
/// </summary>
public class HotelApiService : IHotelApiService
{
    private readonly HttpClient             _http;
    private readonly ILogger<HotelApiService> _logger;

    public HotelApiService(HttpClient http, ILogger<HotelApiService> logger)
    {
        _http   = http;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<HotelResult>> SearchAsync(
        string city, DateTime checkIn, DateTime checkOut)
    {
        try
        {
            _logger.LogInformation(
                "Hotel search: {City} | {In:d} – {Out:d}", city, checkIn, checkOut);

            // In production, uncomment and point at a real endpoint:
            // var url = $"v1/hotels?city={Uri.EscapeDataString(city)}&checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}";
            // return await _http.GetFromJsonAsync<List<HotelResult>>(url) ?? [];

            await Task.Delay(150); // simulate network latency

            return new[]
            {
                new HotelResult($"Hotel Europa {city}",       "Grote Markt 1",    89.99m,  "https://booking.example.com/1"),
                new HotelResult($"City Inn {city}",           "Stationsstraat 12",119.00m, "https://booking.example.com/2"),
                new HotelResult($"Champions Suites {city}",   "Stadionlaan 3",    149.50m, "https://booking.example.com/3"),
                new HotelResult($"Budget Stay {city}",        "Kerkstraat 5",     59.00m,  "https://booking.example.com/4"),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hotel API unavailable; returning empty list");
            return Enumerable.Empty<HotelResult>();
        }
    }
}
