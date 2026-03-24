namespace FullStackDevelopment_Ticketverkoop.Web.Models.ViewModels;

/// <summary>
/// ViewModel for displaying a match in the calendar view.
/// Only contains the data the view actually needs — not the full entity graph.
/// </summary>
public class MatchViewModel
{
    public int Id { get; set; }
    public string HomeClubName { get; set; } = string.Empty;
    public string AwayClubName { get; set; } = string.Empty;
    public string StadiumName { get; set; } = string.Empty;
    public string HomeLogoUrl { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; }
    public string Phase { get; set; } = string.Empty;
}