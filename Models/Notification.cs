namespace FridgeManager.Api.Models;

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public int? FridgeItemId { get; set; }
    public FridgeItem? FridgeItem { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum NotificationType
{
    ExpiryWarning,
    Expired,
    ShoppingListUpdated,
    ForecastReady
}
