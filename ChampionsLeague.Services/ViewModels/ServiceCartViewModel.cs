using System.ComponentModel.DataAnnotations;

namespace ChampionsLeague.Services.ViewModels;

/// <summary>ViewModel/DTO for a singel line of a seasonticket in the cart
public class ServiceSeasonCartItemVM
{
    public int SectorId { get; set; }
    public string SectorName { get; set; } = string.Empty;
    public string StadiumName { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
}
