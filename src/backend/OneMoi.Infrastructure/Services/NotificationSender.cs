using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneMoi.Application.Common;
using OneMoi.Domain.Entities;
using OneMoi.Domain.Enums;
using OneMoi.Infrastructure.Persistence;

namespace OneMoi.Infrastructure.Services;

/// <summary>
/// Sends OTP / notifications.
///  • E-mail: real SMTP if "Smtp:Host" is configured, otherwise simulated.
///  • SMS: simulated (plug in MSG91 / Gupshup with DLT templates before going live).
/// Every message is written to auth.NotificationLogs (with the OTP masked).
/// In development the full text is also printed to the API console.
/// </summary>
public class NotificationSender(AppDbContext db, IConfiguration config, ILogger<NotificationSender> log) : INotificationSender
{
    public async Task SendAsync(OtpChannel channel, string recipient, string? subject, string body, string logBody, int? tenantId = null, CancellationToken ct = default)
    {
        var entry = new NotificationLog
        {
            TenantId = tenantId, Channel = channel, Recipient = recipient, Subject = subject, Body = logBody,
            CreatedAt = DateTime.UtcNow, Status = NotificationStatus.Simulated, Provider = "dev-console"
        };

        try
        {
            if (channel == OtpChannel.Email && !string.IsNullOrWhiteSpace(config["Smtp:Host"]))
            {
                using var smtp = new SmtpClient(config["Smtp:Host"], int.Parse(config["Smtp:Port"] ?? "587"))
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(config["Smtp:User"], config["Smtp:Password"])
                };
                await smtp.SendMailAsync(new MailMessage(config["Smtp:From"] ?? config["Smtp:User"]!, recipient, subject ?? "OneMoi", body), ct);
                entry.Status = NotificationStatus.Sent;
                entry.Provider = "smtp";
            }
            else
            {
                log.LogWarning("📨 [{Channel} → {Recipient}] {Body}", channel, recipient, body);
            }
        }
        catch (Exception ex)
        {
            entry.Status = NotificationStatus.Failed;
            entry.Error = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            log.LogError(ex, "Notification to {Recipient} failed", recipient);
        }

        db.NotificationLogs.Add(entry);
        await db.SaveChangesAsync(ct);
    }
}
