using FridgeManager.Api.Data;
using FridgeManager.Api.DTOs;
using FridgeManager.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FridgeManager.Api.Services;

public class FridgeItemService(AppDbContext db, IShoppingListService shoppingList) : IFridgeItemService
{
    public async Task<List<FridgeItemResponseDto>> GetAllAsync()
    {
        var items = await db.FridgeItems
            .Where(x => x.Status != ItemStatus.Used)
            .OrderBy(x => x.ExpiryDate)
            .ToListAsync();

        return items.Select(MapToResponse).ToList();
    }

    public async Task<FridgeItemResponseDto?> GetByIdAsync(int id)
    {
        var item = await db.FridgeItems.FindAsync(id);
        return item is null ? null : MapToResponse(item);
    }

    public async Task<FridgeItemResponseDto> CreateAsync(CreateFridgeItemDto dto)
    {
        var item = new FridgeItem
        {
            Name = dto.Name,
            Category = dto.Category,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            CostPerUnit = dto.CostPerUnit,
            TotalCost = dto.Quantity * dto.CostPerUnit,
            PurchaseDate = dto.PurchaseDate,
            ExpiryDate = dto.ExpiryDate,
            ExpiryReminderDays = dto.ExpiryReminderDays
        };

        db.FridgeItems.Add(item);
        await db.SaveChangesAsync();
        return MapToResponse(item);
    }

    public async Task<FridgeItemResponseDto?> UpdateAsync(int id, UpdateFridgeItemDto dto)
    {
        var item = await db.FridgeItems.FindAsync(id);
        if (item is null) return null;

        item.Name = dto.Name;
        item.Category = dto.Category;
        item.Quantity = dto.Quantity;
        item.Unit = dto.Unit;
        item.CostPerUnit = dto.CostPerUnit;
        item.TotalCost = dto.Quantity * dto.CostPerUnit;
        item.ExpiryDate = dto.ExpiryDate;
        item.ExpiryReminderDays = dto.ExpiryReminderDays;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return MapToResponse(item);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await db.FridgeItems.FindAsync(id);
        if (item is null) return false;

        db.FridgeItems.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<FridgeItemResponseDto?> MarkAsUsedAsync(int id, MarkUsedDto dto)
    {
        var item = await db.FridgeItems.FindAsync(id);
        if (item is null) return null;

        db.ConsumptionLogs.Add(new ConsumptionLog
        {
            FridgeItemId = id,
            QuantityUsed = dto.QuantityUsed,
            Notes = dto.Notes
        });

        item.Quantity -= dto.QuantityUsed;

        if (item.Quantity <= 0)
        {
            item.Quantity = 0;
            item.Status = ItemStatus.Used;
            item.UpdatedAt = DateTime.UtcNow;

            // auto-add to shopping list
            await shoppingList.AutoAddAsync(item);
        }

        await db.SaveChangesAsync();
        return MapToResponse(item);
    }

    public async Task<WastageReportDto> GetWastageReportAsync(int month, int year)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        var wastedItems = await db.FridgeItems
            .Where(x => (x.Status == ItemStatus.Expired || x.Status == ItemStatus.Wasted)
                     && x.ExpiryDate >= start && x.ExpiryDate < end)
            .ToListAsync();

        return new WastageReportDto(
            month, year,
            wastedItems.Count(x => x.Status == ItemStatus.Expired),
            wastedItems.Count(x => x.Status == ItemStatus.Wasted),
            wastedItems.Sum(x => x.TotalCost),
            wastedItems.Select(x => new WastageItemDto(
                x.Name, x.Category, x.Quantity, x.Unit,
                x.TotalCost, x.ExpiryDate)).ToList()
        );
    }

    public async Task<List<ForecastDto>> GetForecastAsync()
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var logs = await db.ConsumptionLogs
            .Include(x => x.FridgeItem)
            .Where(x => x.UsedAt >= thirtyDaysAgo)
            .ToListAsync();

        return logs
            .GroupBy(x => new { x.FridgeItem.Name, x.FridgeItem.Category, x.FridgeItem.Unit, x.FridgeItem.CostPerUnit })
            .Select(g =>
            {
                var totalUsed = g.Sum(x => x.QuantityUsed);
                var avgWeekly = totalUsed / 4m;
                var avgMonthly = totalUsed;

                return new ForecastDto(
                    g.Key.Name,
                    g.Key.Category,
                    g.Key.Unit,
                    avgWeekly,
                    avgMonthly,
                    Math.Ceiling(avgWeekly * 1.1m),
                    Math.Ceiling(avgMonthly * 1.1m),
                    Math.Round(avgWeekly * 1.1m * g.Key.CostPerUnit, 2),
                    Math.Round(avgMonthly * 1.1m * g.Key.CostPerUnit, 2)
                );
            })
            .ToList();
    }

    public async Task<List<FridgeItemResponseDto>> GetExpiringItemsAsync(int withinDays = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(withinDays);
        var items = await db.FridgeItems
            .Where(x => x.Status == ItemStatus.Active && x.ExpiryDate <= cutoff)
            .OrderBy(x => x.ExpiryDate)
            .ToListAsync();

        return items.Select(MapToResponse).ToList();
    }

    private static FridgeItemResponseDto MapToResponse(FridgeItem item) => new(
        item.Id, item.Name, item.Category,
        item.Quantity, item.Unit, item.CostPerUnit, item.TotalCost,
        item.PurchaseDate, item.ExpiryDate, item.ExpiryReminderDays,
        (int)(item.ExpiryDate - DateTime.UtcNow).TotalDays,
        item.Status, item.CreatedAt
    );
}
