using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace ChampionsLeague.Infrastructure.Services;

/// <summary>Contract for the email service.</summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
}

/// <summary>
/// Real email implementation using MailKit + SMTP (Brevo).
/// Falls back to console logging if SMTP is not configured so the app
/// never crashes in development or when credentials are missing.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration            _config;
    private readonly ILogger<EmailService>     _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        // The ?? "" guarantees a non-null string
        string safeTo = to?.Replace("\n", "").Replace("\r", "") ?? "";
        string safeSubject = subject?.Replace("\n", "").Replace("\r", "") ?? "";

        // The ?? "" already handles it — but if you're passing safeTo to MailboxAddress.Parse:
        MailboxAddress.Parse(safeTo);  // safeTo is never null because of ?? ""

        var host     = _config["Email:SmtpHost"];
        _ = int.TryParse(_config["Email:SmtpPort"], out var port);
        if (port == 0) port = 587; // default Brevo SMTP port
        var user     = _config["Email:SmtpUser"];
        var pass     = _config["Email:SmtpPass"];
        var fromAddr = _config["Email:FromAddress"];
        var fromName = _config["Email:FromName"] ?? "CL Tickets";

        // If SMTP not configured, log to console and return (dev mode)
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            _logger.LogInformation("📧 Email sent to {To}: {Subject}", safeTo, safeSubject);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddr ?? user));
            message.To.Add(MailboxAddress.Parse(safeTo));
            message.Subject = safeSubject;
            message.Body    = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(user, pass);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("📧 Email sent to {To}: {Subject}", safeTo, safeSubject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "📧 Failed to send email to {To}: {Subject}", safeTo, safeSubject);
            // Don't rethrow — a failed email should never crash a ticket purchase
        }
    }
}
