using FridgeManager.Api.Common;
using FridgeManager.Api.Data;
using FridgeManager.Api.Hubs;
using FridgeManager.Api.Models;
using FridgeManager.Api.Models.Identity;
using FridgeManager.Api.Repositories;
using FridgeManager.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FridgeManager.Api.Jobs;

public class ExpiryCheckJob(
    IFridgeItemRepository repository,
    AppDbContext db,
    IHubContext<NotificationHub> hubContext,
    IRecipeService recipeService,
    IEmailService emailService,
    UserManager<ApplicationUser> userManager,
    IDateTimeProvider dateTime,
    ILogger<ExpiryCheckJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Expiry check job started at {Time}", dateTime.UtcNow);
        var today = dateTime.UtcNow.Date;

        var grouped = await repository.GetAllActiveGroupedByUserAsync(ct);
        logger.LogInformation("Processing {UserCount} users", grouped.Count);

        foreach (var (userId, items) in grouped)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user is null) continue;

            var emailItems = new List<ExpiryEmailItem>();

            foreach (var item in items)
            {
                var daysLeft = (item.ExpiryDate.Date - today).Days;

                if (daysLeft < 0)
                {
                    item.Status = ItemStatus.Expired;
                    item.UpdatedAt = dateTime.UtcNow;

                    db.Notifications.Add(new Notification
                    {
                        UserId = userId,
                        Title = $"{item.Name} has expired!",
                        Message = $"Your {item.Name} ({item.Quantity} {item.Unit}) expired on {item.ExpiryDate:MMM dd}.",
                        Type = NotificationType.Expired,
                        FridgeItemId = item.Id,
                        CreatedAt = dateTime.UtcNow
                    });

                    emailItems.Add(new ExpiryEmailItem(item.Name, daysLeft, item.Unit, item.Quantity, "Consider discarding this item."));
                    await hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", $"{item.Name} has expired!", ct);
                    logger.LogWarning("Item {ItemId} ({Name}) expired for user {UserId}", item.Id, item.Name, userId);
                    continue;
                }

                if (daysLeft <= item.ExpiryReminderDays)
                {
                    var alreadyNotified = await db.Notifications.AnyAsync(n =>
                        n.UserId == userId &&
                        n.FridgeItemId == item.Id &&
                        n.Type == NotificationType.ExpiryWarning &&
                        n.CreatedAt.Date == today, ct);

                    if (!alreadyNotified)
                    {
                        var expiringNames = items
                            .Where(x => (x.ExpiryDate.Date - today).Days <= x.ExpiryReminderDays)
                            .Select(x => x.Name).ToList();

                        var recipes = await recipeService.SuggestRecipesAsync(expiringNames);
                        var recipeSuggestion = recipes.FirstOrDefault()?.Title ?? "Check recipe suggestions in the app.";

                        db.Notifications.Add(new Notification
                        {
                            UserId = userId,
                            Title = $"{item.Name} expires in {daysLeft} day(s)!",
                            Message = $"Use it soon. Try: {recipeSuggestion}",
                            Type = NotificationType.ExpiryWarning,
                            FridgeItemId = item.Id,
                            CreatedAt = dateTime.UtcNow
                        });

                        emailItems.Add(new ExpiryEmailItem(item.Name, daysLeft, item.Unit, item.Quantity, recipeSuggestion));
                        await hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", $"{item.Name} expires in {daysLeft} day(s)!", ct);
                        logger.LogInformation("Expiry reminder for item {ItemId} ({Name}), user {UserId}", item.Id, item.Name, userId);
                    }
                }
            }

            // send one consolidated email per user for all expiring items
            if (emailItems.Count > 0 && user.Email is not null)
            {
                await emailService.SendExpiryNotificationAsync(user.Email, user.FirstName, emailItems, ct);
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Expiry check job completed.");
    }
}
