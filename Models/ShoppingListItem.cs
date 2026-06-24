namespace FridgeManager.Api.Models;

public class ShoppingListItem
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public bool IsAutoAdded { get; set; }
    public bool IsPurchased { get; set; }
    public DateTime CreatedAt { get; set; }
}
