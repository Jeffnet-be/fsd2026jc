using System.ComponentModel.DataAnnotations;

namespace ChampionsLeague.Web.ViewModels;

/// <summary>
/// Hotel search page ViewModel — binds both the search form inputs and the results list.
/// The same object is passed back to the view after a POST so the form retains its values.
/// </summary>
public class HotelSearchVM
{
    [Required(ErrorMessage = "Please enter a city.")]
    public string City { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    [Display(Name = "Check-in")]
    public DateTime CheckIn  { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date)]
    [Display(Name = "Check-out")]
    public DateTime CheckOut { get; set; } = DateTime.Today.AddDays(1);

    /// <summary>Results populated after a successful search — empty on the initial GET.</summary>
    public IEnumerable<HotelResultVM> Results { get; set; } = Enumerable.Empty<HotelResultVM>();
}

/// <summary>One hotel result row returned by the external API stub.</summary>
public class HotelResultVM
{
    public string  Name          { get; set; } = string.Empty;
    public string  Address       { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public string  BookingUrl    { get; set; } = string.Empty;
}
