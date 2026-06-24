namespace FridgeManager.Api.Models;

public class ShoppingListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public bool IsAutoAdded { get; set; } // true = added when item was marked used
    public bool IsPurchased { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
