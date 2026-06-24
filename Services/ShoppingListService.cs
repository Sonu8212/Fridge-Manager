using FridgeManager.Api.Data;
using FridgeManager.Api.DTOs;
using FridgeManager.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FridgeManager.Api.Services;

public class ShoppingListService(AppDbContext db) : IShoppingListService
{
    public async Task<List<ShoppingItemResponseDto>> GetAllAsync()
    {
        var items = await db.ShoppingListItems
            .Where(x => !x.IsPurchased)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        return items.Select(MapToResponse).ToList();
    }

    public async Task<ShoppingItemResponseDto> AddAsync(CreateShoppingItemDto dto)
    {
        var item = new ShoppingListItem
        {
            Name = dto.Name,
            Category = dto.Category,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            EstimatedCost = dto.EstimatedCost,
            IsAutoAdded = false
        };

        db.ShoppingListItems.Add(item);
        await db.SaveChangesAsync();
        return MapToResponse(item);
    }

    public async Task AutoAddAsync(FridgeItem fridgeItem)
    {
        // avoid duplicates — check if already in shopping list
        var exists = await db.ShoppingListItems
            .AnyAsync(x => x.Name == fridgeItem.Name && !x.IsPurchased);

        if (!exists)
        {
            db.ShoppingListItems.Add(new ShoppingListItem
            {
                Name = fridgeItem.Name,
                Category = fridgeItem.Category,
                Quantity = fridgeItem.Quantity == 0 ? 1 : fridgeItem.Quantity,
                Unit = fridgeItem.Unit,
                EstimatedCost = fridgeItem.CostPerUnit,
                IsAutoAdded = true
            });
        }
    }

    public async Task<bool> MarkPurchasedAsync(int id)
    {
        var item = await db.ShoppingListItems.FindAsync(id);
        if (item is null) return false;

        item.IsPurchased = true;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await db.ShoppingListItems.FindAsync(id);
        if (item is null) return false;

        db.ShoppingListItems.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }

    private static ShoppingItemResponseDto MapToResponse(ShoppingListItem item) => new(
        item.Id, item.Name, item.Category,
        item.Quantity, item.Unit, item.EstimatedCost,
        item.IsAutoAdded, item.IsPurchased, item.CreatedAt
    );
}
