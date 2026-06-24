using FridgeManager.Api.Models;

namespace FridgeManager.Api.DTOs;

public record CreateFridgeItemDto(
    string Name,
    string Category,
    decimal Quantity,
    string Unit,
    decimal CostPerUnit,
    DateTime PurchaseDate,
    DateTime ExpiryDate,
    int ExpiryReminderDays = 3
);

public record UpdateFridgeItemDto(
    string Name,
    string Category,
    decimal Quantity,
    string Unit,
    decimal CostPerUnit,
    DateTime ExpiryDate,
    int ExpiryReminderDays
);

public record FridgeItemResponseDto(
    int Id,
    string Name,
    string Category,
    decimal Quantity,
    string Unit,
    decimal CostPerUnit,
    decimal TotalCost,
    DateTime PurchaseDate,
    DateTime ExpiryDate,
    int ExpiryReminderDays,
    int DaysUntilExpiry,
    ItemStatus Status,
    DateTime CreatedAt
);

public record MarkUsedDto(
    decimal QuantityUsed,
    string Notes = ""
);

public record WastageReportDto(
    int Month,
    int Year,
    int TotalItemsExpired,
    int TotalItemsWasted,
    decimal TotalCostWasted,
    List<WastageItemDto> WastedItems
);

public record WastageItemDto(
    string Name,
    string Category,
    decimal Quantity,
    string Unit,
    decimal CostLost,
    DateTime ExpiryDate
);

public record ForecastDto(
    string Name,
    string Category,
    string Unit,
    decimal AverageWeeklyConsumption,
    decimal AverageMonthlyConsumption,
    decimal ForecastedWeeklyNeed,
    decimal ForecastedMonthlyNeed,
    decimal EstimatedWeeklyCost,
    decimal EstimatedMonthlyCost
);
