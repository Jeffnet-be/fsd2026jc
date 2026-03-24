using System.ComponentModel.DataAnnotations;

namespace FullStackDevelopment_Ticketverkoop.Web.Models.ViewModels;

/// <summary>
/// ViewModel for adding tickets to the shopping cart.
/// Data annotations handle server-side validation before business rules run.
/// </summary>
public class CartViewModel
{
    [Required]
    public int MatchId { get; set; }

    [Required]
    public int SectionTypeId { get; set; }

    [Required]
    [Range(1, 4, ErrorMessage = "You can add between 1 and 4 tickets at a time.")]
    public int Quantity { get; set; }

    // Display helpers (filled by controller, not submitted by form)
    public string MatchDescription { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}