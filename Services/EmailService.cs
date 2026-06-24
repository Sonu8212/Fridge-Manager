using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FridgeManager.Api.Services;

public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendVerificationEmailAsync(string toEmail, string firstName, string verificationLink, CancellationToken ct = default)
    {
        var subject = "Verify your FridgeManager account";
        var html = $"""
            <h2>Welcome to FridgeManager, {firstName}!</h2>
            <p>Please verify your email address by clicking the button below.</p>
            <p>
              <a href="{verificationLink}"
                 style="background:#2563eb;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:bold;">
                Verify Email
              </a>
            </p>
            <p>Or copy this link into your browser:</p>
            <p><a href="{verificationLink}">{verificationLink}</a></p>
            <p>This link expires in 24 hours. If you did not register, you can ignore this email.</p>
            """;

        await SendAsync(toEmail, subject, html, ct);
    }

    public async Task SendExpiryNotificationAsync(string toEmail, string firstName, List<ExpiryEmailItem> items, CancellationToken ct = default)
    {
        var subject = $"FridgeManager — {items.Count} item(s) expiring soon!";

        var rows = string.Join("", items.Select(i =>
            $"""
            <tr>
              <td style="padding:8px;border-bottom:1px solid #e5e7eb;">{i.Name}</td>
              <td style="padding:8px;border-bottom:1px solid #e5e7eb;">{i.Quantity} {i.Unit}</td>
              <td style="padding:8px;border-bottom:1px solid #e5e7eb;color:{(i.DaysLeft <= 1 ? "#dc2626" : "#d97706")};">
                {(i.DaysLeft <= 0 ? "Expired!" : $"{i.DaysLeft} day(s)")}
              </td>
              <td style="padding:8px;border-bottom:1px solid #e5e7eb;">{i.RecipeSuggestion}</td>
            </tr>
            """));

        var html = $"""
            <h2>Hi {firstName}, some items in your fridge need attention!</h2>
            <table style="border-collapse:collapse;width:100%;font-family:sans-serif;">
              <thead>
                <tr style="background:#f3f4f6;">
                  <th style="padding:8px;text-align:left;">Item</th>
                  <th style="padding:8px;text-align:left;">Quantity</th>
                  <th style="padding:8px;text-align:left;">Expires</th>
                  <th style="padding:8px;text-align:left;">Recipe Suggestion</th>
                </tr>
              </thead>
              <tbody>{rows}</tbody>
            </table>
            <p style="margin-top:16px;">Log in to mark items as used or check your shopping list.</p>
            """;

        await SendAsync(toEmail, subject, html, ct);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var settings = config.GetSection("Email");
        var host = settings["Host"];
        var port = int.Parse(settings["Port"] ?? "587");
        var username = settings["Username"];
        var password = settings["Password"];
        var from = settings["From"] ?? username;
        var displayName = settings["DisplayName"] ?? "FridgeManager";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(displayName, from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(username, password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
            logger.LogInformation("Email sent to {Email} — subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            // do not rethrow — email failure must not fail the main flow
        }
    }
}
