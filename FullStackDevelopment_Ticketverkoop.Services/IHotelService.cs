namespace FullStackDevelopment_Ticketverkoop.Services;

public record HotelResult(string Name, string City, decimal PricePerNight, string BookingUrl);

/// <summary>
/// Contract for retrieving hotel suggestions near a match venue.
/// Uses an external public API — results are displayed only, not stored.
/// </summary>
public interface IHotelService
{
    Task<IEnumerable<HotelResult>> SearchHotelsAsync(string city, DateTime checkIn, DateTime checkOut);
}