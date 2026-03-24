using FullStackDevelopment_Ticketverkoop.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace FullStackDevelopment_Ticketverkoop.Services
{
    /// <summary>
    /// Sends emails via SMTP. Connection settings are read from appsettings.json
    /// so no credentials are hard-coded in the source.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config) => _config = config;

        /// <summary>
        /// Builds and sends a voucher email listing all purchased tickets.
        /// Each ticket's unique VoucherId is included in the body.
        /// </summary>
        public async Task SendVoucherEmailAsync(string userId, IEnumerable<Ticket> tickets)
        {
            var smtp = BuildSmtpClient();
            var body = new StringBuilder();
            body.AppendLine("<h2>Your Champions League Tickets</h2>");
            body.AppendLine("<ul>");
            foreach (var t in tickets)
            {
                body.AppendLine($"<li>Match: {t.Match?.HomeClub?.Name} vs {t.Match?.AwayClub?.Name} " +
                                $"| Section: {t.SectionType?.Name} " +
                                $"| Seat: {t.SeatRow}{t.SeatNumber} " +
                                $"| <strong>Voucher: {t.VoucherId}</strong></li>");
            }
            body.AppendLine("</ul>");

            var mail = new MailMessage(
                from: _config["Email:From"] ?? "noreply@cltickets.be",
                to: userId, // In production this would be the user's email address
                subject: "Your Ticket Voucher(s) – Champions League",
                body: body.ToString())
            {
                IsBodyHtml = true
            };

            try { await smtp.SendMailAsync(mail); }
            catch { /* Log in production; don't crash the purchase flow */ }
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            var smtp = BuildSmtpClient();
            var mail = new MailMessage(
                _config["Email:From"] ?? "noreply@cltickets.be",
                email,
                "Password Reset – Champions League Tickets",
                $"<p>Click <a href='{resetLink}'>here</a> to reset your password.</p>")
            {
                IsBodyHtml = true
            };
            try { await smtp.SendMailAsync(mail); }
            catch { /* Log in production */ }
        }

        private SmtpClient BuildSmtpClient() => new(_config["Email:Host"] ?? "localhost")
        {
            Port = int.Parse(_config["Email:Port"] ?? "587"),
            Credentials = new NetworkCredential(
                _config["Email:User"],
                _config["Email:Password"]),
            EnableSsl = true,
        };
    }
}