using Microsoft.Extensions.Logging;

namespace ChampionsLeague.Infrastructure.Services;

/// <summary>
/// Contract for the e-mail service. Keeping this as an interface means the
/// implementation can be swapped (e.g., MailKit, SendGrid) without touching callers.
/// </summary>
public interface IEmailService
{
    /// <summary>Sends an HTML e-mail to the given address.</summary>
    Task SendAsync(string to, string subject, string htmlBody);
}

/// <summary>
/// Stub e-mail service: writes a formatted message to the .NET logger instead of
/// sending a real e-mail. This is the correct pattern for demo/development mode.
/// Replace the body of SendAsync with MailKit or SendGrid for production.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger) => _logger = logger;

    /// <inheritdoc/>
    public Task SendAsync(string to, string subject, string htmlBody)
    {
        _logger.LogInformation(
            "📧 [EMAIL STUB] To: {To} | Subject: {Subject}\n{Body}",
            to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
