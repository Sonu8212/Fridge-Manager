namespace FridgeManager.Api.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string firstName, string verificationLink, CancellationToken ct = default);
    Task SendExpiryNotificationAsync(string toEmail, string firstName, List<ExpiryEmailItem> items, CancellationToken ct = default);
}

public record ExpiryEmailItem(string Name, int DaysLeft, string Unit, decimal Quantity, string RecipeSuggestion);
