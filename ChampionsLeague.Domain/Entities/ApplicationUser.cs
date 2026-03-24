using Microsoft.AspNetCore.Identity;

namespace ChampionsLeague.Domain.Entities;

/// <summary>
/// Extends ASP.NET Core Identity IdentityUser with domain-specific profile fields.
/// By inheriting IdentityUser we get authentication, password hashing,
/// and role management for free — no custom security code needed.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>First name of the registered supporter.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Last name of the registered supporter.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Full display name, computed from first + last name.</summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>Navigation: all orders placed by this user.</summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
