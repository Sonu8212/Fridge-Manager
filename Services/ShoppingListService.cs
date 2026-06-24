using ErrorOr;
using FridgeManager.Api.Common;
using FridgeManager.Api.DTOs;
using FridgeManager.Api.Models;
using FridgeManager.Api.Repositories;

namespace FridgeManager.Api.Services;

public class ShoppingListService(
    IShoppingListRepository repository,
    IDateTimeProvider dateTime,
    ILogger<ShoppingListService> logger) : IShoppingListService
{
    public async Task<List<ShoppingItemResponseDto>> GetAllAsync(string userId, CancellationToken ct = default)
    {
        var items = await repository.GetPendingAsync(userId, ct);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<ShoppingItemResponseDto> AddAsync(string userId, CreateShoppingItemDto dto, CancellationToken ct = default)
    {
        var item = new ShoppingListItem
        {
            UserId = userId,
            Name = dto.Name,
            Category = dto.Category,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            EstimatedCost = dto.EstimatedCost,
            IsAutoAdded = false,
            CreatedAt = dateTime.UtcNow
        };
        await repository.AddAsync(item, ct);
        logger.LogInformation("User {UserId} manually added {Name} to shopping list", userId, dto.Name);
        return MapToResponse(item);
    }

    public async Task AutoAddAsync(string userId, FridgeItem fridgeItem, CancellationToken ct = default)
    {
        var exists = await repository.ExistsByNameAsync(userId, fridgeItem.Name, ct);
        if (exists) return;

        await repository.AddAsync(new ShoppingListItem
        {
            UserId = userId,
            Name = fridgeItem.Name,
            Category = fridgeItem.Category,
            Quantity = fridgeItem.Quantity == 0 ? 1 : fridgeItem.Quantity,
            Unit = fridgeItem.Unit,
            EstimatedCost = fridgeItem.CostPerUnit,
            IsAutoAdded = true,
            CreatedAt = dateTime.UtcNow
        }, ct);
        logger.LogInformation("Auto-added {Name} to shopping list for user {UserId}", fridgeItem.Name, userId);
    }

    public async Task<ErrorOr<Updated>> MarkPurchasedAsync(string userId, int id, CancellationToken ct = default)
    {
        var item = await repository.GetByIdAsync(id, userId, ct);
        if (item is null) return Errors.ShoppingItem.NotFound(id);
        item.IsPurchased = true;
        await repository.SaveChangesAsync(ct);
        return Result.Updated;
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(string userId, int id, CancellationToken ct = default)
    {
        var item = await repository.GetByIdAsync(id, userId, ct);
        if (item is null) return Errors.ShoppingItem.NotFound(id);
        await repository.DeleteAsync(item, ct);
        return Result.Deleted;
    }

    private static ShoppingItemResponseDto MapToResponse(ShoppingListItem item) => new(
        item.Id, item.Name, item.Category,
        item.Quantity, item.Unit, item.EstimatedCost,
        item.IsAutoAdded, item.IsPurchased, item.CreatedAt
    );
}
