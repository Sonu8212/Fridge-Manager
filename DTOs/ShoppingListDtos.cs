namespace FridgeManager.Api.DTOs;

public record CreateShoppingItemDto(
    string Name,
    string Category,
    decimal Quantity,
    string Unit,
    decimal EstimatedCost
);

public record ShoppingItemResponseDto(
    int Id,
    string Name,
    string Category,
    decimal Quantity,
    string Unit,
    decimal EstimatedCost,
    bool IsAutoAdded,
    bool IsPurchased,
    DateTime CreatedAt
);
