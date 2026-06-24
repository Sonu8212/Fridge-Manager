namespace FridgeManager.Api.Models;

public class FridgeItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty; // kg, L, pcs, g, ml, etc.
    public decimal CostPerUnit { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int ExpiryReminderDays { get; set; } = 3; // notify X days before expiry
    public ItemStatus Status { get; set; } = ItemStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ConsumptionLog> ConsumptionLogs { get; set; } = [];
}

public enum ItemStatus
{
    Active,
    Used,
    Expired,
    Wasted
}
