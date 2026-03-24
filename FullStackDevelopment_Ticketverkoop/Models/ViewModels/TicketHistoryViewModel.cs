namespace FullStackDevelopment_Ticketverkoop.Web.Models.ViewModels;

/// <summary>
/// ViewModel for a single entry in the user's ticket purchase history.
/// </summary>
public class TicketHistoryViewModel
{
    public int TicketId { get; set; }
    public string MatchDescription { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string SeatRow { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public decimal Price { get; set; }
    public string? VoucherId { get; set; }
    public string Status { get; set; } = string.Empty;
}