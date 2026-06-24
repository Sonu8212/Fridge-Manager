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
    public async Task<List<ShoppingItemResponseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await repository.GetPendingAsync(ct);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<ShoppingItemResponseDto> AddAsync(CreateShoppingItemDto dto, CancellationToken ct = default)
    {
        var item = new ShoppingListItem
        {
            Name = dto.Name,
            Category = dto.Category,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            EstimatedCost = dto.EstimatedCost,
            IsAutoAdded = false,
            CreatedAt = dateTime.UtcNow
        };

        await repository.AddAsync(item, ct);
        logger.LogInformation("Manually added {Name} to shopping list", dto.Name);
        return MapToResponse(item);
    }

    public async Task AutoAddAsync(FridgeItem fridgeItem, CancellationToken ct = default)
    {
        var exists = await repository.ExistsByNameAsync(fridgeItem.Name, ct);
        if (exists) return;

        var item = new ShoppingListItem
        {
            Name = fridgeItem.Name,
            Category = fridgeItem.Category,
            Quantity = fridgeItem.Quantity == 0 ? 1 : fridgeItem.Quantity,
            Unit = fridgeItem.Unit,
            EstimatedCost = fridgeItem.CostPerUnit,
            IsAutoAdded = true,
            CreatedAt = dateTime.UtcNow
        };

        await repository.AddAsync(item, ct);
        logger.LogInformation("Auto-added {Name} to shopping list after full consumption", fridgeItem.Name);
    }

    public async Task<ErrorOr<Updated>> MarkPurchasedAsync(int id, CancellationToken ct = default)
    {
        var item = await repository.GetByIdAsync(id, ct);
        if (item is null) return Errors.ShoppingItem.NotFound(id);

        item.IsPurchased = true;
        await repository.SaveChangesAsync(ct);
        return Result.Updated;
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var item = await repository.GetByIdAsync(id, ct);
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
