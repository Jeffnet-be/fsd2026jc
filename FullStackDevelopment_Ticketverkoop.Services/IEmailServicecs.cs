using FullStackDevelopment_Ticketverkoop.Domain.Entities;

namespace FullStackDevelopment_Ticketverkoop.Services;

/// <summary>
/// Contract for sending transactional emails (vouchers, password reset).
/// Decoupled via interface so a mock implementation can be used during testing.
/// </summary>
public interface IEmailService
{
    Task SendVoucherEmailAsync(string userId, IEnumerable<Ticket> tickets);
    Task SendPasswordResetEmailAsync(string email, string resetLink);
}