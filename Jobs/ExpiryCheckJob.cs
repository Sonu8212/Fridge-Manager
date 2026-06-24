using FridgeManager.Api.Common;
using FridgeManager.Api.Data;
using FridgeManager.Api.Hubs;
using FridgeManager.Api.Models;
using FridgeManager.Api.Repositories;
using FridgeManager.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FridgeManager.Api.Jobs;

public class ExpiryCheckJob(
    IFridgeItemRepository repository,
    AppDbContext db,
    IHubContext<NotificationHub> hubContext,
    IRecipeService recipeService,
    IDateTimeProvider dateTime,
    ILogger<ExpiryCheckJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Expiry check job started at {Time}", dateTime.UtcNow);
        var today = dateTime.UtcNow.Date;
        var items = await repository.GetActiveAsync(ct);

        foreach (var item in items)
        {
            var daysLeft = (item.ExpiryDate.Date - today).Days;

            if (daysLeft < 0)
            {
                item.Status = ItemStatus.Expired;
                item.UpdatedAt = dateTime.UtcNow;

                var note = new Notification
                {
                    Title = $"{item.Name} has expired!",
                    Message = $"Your {item.Name} ({item.Quantity} {item.Unit}) expired on {item.ExpiryDate:MMM dd}.",
                    Type = NotificationType.Expired,
                    FridgeItemId = item.Id,
                    CreatedAt = dateTime.UtcNow
                };
                db.Notifications.Add(note);
                await hubContext.Clients.All.SendAsync("ReceiveNotification", note.Title, note.Message, ct);
                logger.LogWarning("Item {ItemId} ({Name}) has expired", item.Id, item.Name);
                continue;
            }

            if (daysLeft <= item.ExpiryReminderDays)
            {
                var alreadyNotified = await db.Notifications.AnyAsync(n =>
                    n.FridgeItemId == item.Id &&
                    n.Type == NotificationType.ExpiryWarning &&
                    n.CreatedAt.Date == today, ct);

                if (!alreadyNotified)
                {
                    var expiringNames = items
                        .Where(x => (x.ExpiryDate.Date - today).Days <= x.ExpiryReminderDays)
                        .Select(x => x.Name)
                        .ToList();

                    var recipes = await recipeService.SuggestRecipesAsync(expiringNames);
                    var recipeTitles = string.Join(", ", recipes.Take(2).Select(r => r.Title));

                    var note = new Notification
                    {
                        Title = $"{item.Name} expires in {daysLeft} day(s)!",
                        Message = $"Use it soon. Try: {recipeTitles}",
                        Type = NotificationType.ExpiryWarning,
                        FridgeItemId = item.Id,
                        CreatedAt = dateTime.UtcNow
                    };
                    db.Notifications.Add(note);
                    await hubContext.Clients.All.SendAsync("ReceiveNotification", note.Title, note.Message, ct);
                    logger.LogInformation("Expiry reminder sent for item {ItemId} ({Name}), {Days} days left", item.Id, item.Name, daysLeft);
                }
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Expiry check job completed. Processed {Count} items.", items.Count);
    }
}
