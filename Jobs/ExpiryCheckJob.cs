using FridgeManager.Api.Data;
using FridgeManager.Api.Hubs;
using FridgeManager.Api.Models;
using FridgeManager.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FridgeManager.Api.Jobs;

public class ExpiryCheckJob(
    AppDbContext db,
    IHubContext<NotificationHub> hubContext,
    IRecipeService recipeService)
{
    public async Task RunAsync()
    {
        var today = DateTime.UtcNow.Date;

        var itemsToCheck = await db.FridgeItems
            .Where(x => x.Status == ItemStatus.Active)
            .ToListAsync();

        foreach (var item in itemsToCheck)
        {
            var daysLeft = (item.ExpiryDate.Date - today).Days;

            // mark expired items
            if (daysLeft < 0)
            {
                item.Status = ItemStatus.Expired;
                item.UpdatedAt = DateTime.UtcNow;

                var expiredNote = new Notification
                {
                    Title = $"{item.Name} has expired!",
                    Message = $"Your {item.Name} ({item.Quantity} {item.Unit}) expired on {item.ExpiryDate:MMM dd}.",
                    Type = NotificationType.Expired,
                    FridgeItemId = item.Id
                };
                db.Notifications.Add(expiredNote);
                await hubContext.Clients.All.SendAsync("ReceiveNotification", expiredNote.Title, expiredNote.Message);
                continue;
            }

            // send reminder if within the configured reminder window
            if (daysLeft <= item.ExpiryReminderDays)
            {
                var alreadyNotified = await db.Notifications.AnyAsync(n =>
                    n.FridgeItemId == item.Id &&
                    n.Type == NotificationType.ExpiryWarning &&
                    n.CreatedAt.Date == today);

                if (!alreadyNotified)
                {
                    var expiringIngredients = itemsToCheck
                        .Where(x => (x.ExpiryDate.Date - today).Days <= x.ExpiryReminderDays)
                        .Select(x => x.Name)
                        .ToList();

                    var recipes = await recipeService.SuggestRecipesAsync(expiringIngredients);
                    var recipeTitles = string.Join(", ", recipes.Take(2).Select(r => r.Title));

                    var notification = new Notification
                    {
                        Title = $"{item.Name} expires in {daysLeft} day(s)!",
                        Message = $"Use it soon. Try: {recipeTitles}",
                        Type = NotificationType.ExpiryWarning,
                        FridgeItemId = item.Id
                    };
                    db.Notifications.Add(notification);
                    await hubContext.Clients.All.SendAsync("ReceiveNotification", notification.Title, notification.Message);
                }
            }
        }

        await db.SaveChangesAsync();
    }
}
