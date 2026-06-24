namespace FridgeManager.Api.Models;

public class ConsumptionLog
{
    public int Id { get; set; }
    public int FridgeItemId { get; set; }
    public FridgeItem FridgeItem { get; set; } = null!;
    public decimal QuantityUsed { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;
}
