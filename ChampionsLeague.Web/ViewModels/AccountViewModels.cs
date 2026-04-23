using System.ComponentModel.DataAnnotations;

namespace ChampionsLeague.Web.ViewModels;

/// <summary>
/// Registration form ViewModel.
/// Data Annotations drive both server-side (ModelState) and client-side validation.
/// </summary>
public class RegisterVM
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be 2–50 characters.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be 2–50 characters.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid e-mail address.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Login form ViewModel.</summary>
public class LoginVM
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }
}

/// <summary>Ticket history item shown on the My Tickets page — mapped via AutoMapper.</summary>
public class TicketHistoryItemVM
{
    public int      Id               { get; set; }
    public string   MatchDescription { get; set; } = string.Empty;
    public DateTime MatchDate        { get; set; }
    public string   SectorName       { get; set; } = string.Empty;
    public int      SeatNumber       { get; set; }
    public decimal  PricePaid        { get; set; }
    public Guid     VoucherId        { get; set; }
    public string   Status           { get; set; } = string.Empty;
    public bool     IsCancellable    { get; set; }
}

/// <summary>ViewModel for the Forgot Password form.</summary>
public class ForgotPasswordVM
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid e-mail address.")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>ViewModel for the Reset Password form (token arrives via email link).</summary>
public class ResetPasswordVM
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Viewmodel for the SeasonTicketHistory section </summary>
public class SeasonTicketHistoryVM
{
    public int Id { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public string StadiumName { get; set; } = string.Empty;
    public string SectorName { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime PurchasedAt { get; set; }
    public bool IsActive { get; set; }
    public Guid VoucherId { get; set; }
}
