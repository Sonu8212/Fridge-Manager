using ErrorOr;
using FridgeManager.Api.Common;
using FridgeManager.Api.Data;
using FridgeManager.Api.DTOs;
using FridgeManager.Api.Models;
using FridgeManager.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FridgeManager.Api.Services;

public class FridgeItemService(
    IFridgeItemRepository repository,
    IShoppingListService shoppingList,
    IDateTimeProvider dateTime,
    ILogger<FridgeItemService> logger,
    AppDbContext db) : IFridgeItemService
{
    public async Task<PagedResult<FridgeItemResponseDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await repository.GetPagedAsync(page, pageSize, ct);
        var dtos = items.Select(x => MapToResponse(x, dateTime.UtcNow)).ToList();
        return new PagedResult<FridgeItemResponseDto>(dtos, total, page, pageSize);
    }

    public async Task<ErrorOr<FridgeItemResponseDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await repository.GetByIdAsync(id, ct);
        return item is null ? Errors.FridgeItem.NotFound(id) : MapToResponse(item, dateTime.UtcNow);
    }

    public async Task<FridgeItemResponseDto> CreateAsync(CreateFridgeItemDto dto, CancellationToken ct = default)
    {
        var item = new FridgeItem
        {
            Name = dto.Name,
            Category = dto.Category,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            CostPerUnit = dto.CostPerUnit,
            PurchaseDate = dto.PurchaseDate,
            ExpiryDate = dto.ExpiryDate,
            ExpiryReminderDays = dto.ExpiryReminderDays,
            CreatedAt = dateTime.UtcNow,
            UpdatedAt = dateTime.UtcNow
        };

        await repository.AddAsync(item, ct);
        logger.LogInformation("Created fridge item {ItemId} ({Name})", item.Id, item.Name);
        return MapToResponse(item, dateTime.UtcNow);
    }

    public async Task<ErrorOr<FridgeItemResponseDto>> UpdateAsync(int id, UpdateFridgeItemDto dto, CancellationToken ct = default)
    {
        var item = await repository.GetByIdAsync(id, ct);
        if (item is null) return Errors.FridgeItem.NotFound(id);

        item.Name = dto.Name;
        item.Category = dto.Category;
        item.Quantity = dto.Quantity;
        item.Unit = dto.Unit;
        item.CostPerUnit = dto.CostPerUnit;
        item.ExpiryDate = dto.ExpiryDate;
        item.ExpiryReminderDays = dto.ExpiryReminderDays;
        item.UpdatedAt = dateTime.UtcNow;

        await repository.SaveChangesAsync(ct);
        logger.LogInformation("Updated fridge item {ItemId}", id);
        return MapToResponse(item, dateTime.UtcNow);
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var item = await repository.GetByIdAsync(id, ct);
        if (item is null) return Errors.FridgeItem.NotFound(id);

        await repository.DeleteAsync(item, ct);
        logger.LogInformation("Deleted fridge item {ItemId}", id);
        return Result.Deleted;
    }

    public async Task<ErrorOr<FridgeItemResponseDto>> MarkAsUsedAsync(int id, MarkUsedDto dto, CancellationToken ct = default)
    {
        var item = await repository.GetByIdAsync(id, ct);
        if (item is null) return Errors.FridgeItem.NotFound(id);
        if (item.Status == ItemStatus.Used) return Errors.FridgeItem.AlreadyUsed;
        if (dto.QuantityUsed > item.Quantity) return Errors.FridgeItem.QuantityExceedsStock(item.Quantity);

        db.ConsumptionLogs.Add(new ConsumptionLog
        {
            FridgeItemId = id,
            QuantityUsed = dto.QuantityUsed,
            Notes = dto.Notes,
            UsedAt = dateTime.UtcNow
        });

        item.Quantity -= dto.QuantityUsed;
        item.UpdatedAt = dateTime.UtcNow;

        if (item.Quantity <= 0)
        {
            item.Quantity = 0;
            item.Status = ItemStatus.Used;
            await shoppingList.AutoAddAsync(item, ct);
            logger.LogInformation("Item {ItemId} fully consumed. Auto-added to shopping list.", id);
        }

        await repository.SaveChangesAsync(ct);
        return MapToResponse(item, dateTime.UtcNow);
    }

    public async Task<WastageReportDto> GetWastageReportAsync(int month, int year, CancellationToken ct = default)
    {
        var wastedItems = await repository.GetWastedInMonthAsync(month, year, ct);

        return new WastageReportDto(
            month, year,
            wastedItems.Count(x => x.Status == ItemStatus.Expired),
            wastedItems.Count(x => x.Status == ItemStatus.Wasted),
            wastedItems.Sum(x => x.CostPerUnit * x.Quantity),
            wastedItems.Select(x => new WastageItemDto(
                x.Name, x.Category, x.Quantity, x.Unit,
                x.CostPerUnit * x.Quantity, x.ExpiryDate)).ToList()
        );
    }

    public async Task<List<ForecastDto>> GetForecastAsync(CancellationToken ct = default)
    {
        var thirtyDaysAgo = dateTime.UtcNow.AddDays(-30);

        var forecast = await db.ConsumptionLogs
            .Where(x => x.UsedAt >= thirtyDaysAgo)
            .GroupBy(x => new
            {
                x.FridgeItem.Name,
                x.FridgeItem.Category,
                x.FridgeItem.Unit,
                x.FridgeItem.CostPerUnit
            })
            .Select(g => new
            {
                g.Key.Name,
                g.Key.Category,
                g.Key.Unit,
                g.Key.CostPerUnit,
                TotalUsed = g.Sum(x => x.QuantityUsed)
            })
            .ToListAsync(ct);

        return forecast.Select(g =>
        {
            var avgWeekly = g.TotalUsed / 4m;
            var avgMonthly = g.TotalUsed;
            return new ForecastDto(
                g.Name, g.Category, g.Unit,
                avgWeekly, avgMonthly,
                Math.Ceiling(avgWeekly * 1.1m),
                Math.Ceiling(avgMonthly * 1.1m),
                Math.Round(avgWeekly * 1.1m * g.CostPerUnit, 2),
                Math.Round(avgMonthly * 1.1m * g.CostPerUnit, 2)
            );
        }).ToList();
    }

    public async Task<List<FridgeItemResponseDto>> GetExpiringItemsAsync(int withinDays = 7, CancellationToken ct = default)
    {
        var items = await repository.GetExpiringAsync(withinDays, ct);
        return items.Select(x => MapToResponse(x, dateTime.UtcNow)).ToList();
    }

    private static FridgeItemResponseDto MapToResponse(FridgeItem item, DateTime now) => new(
        item.Id, item.Name, item.Category,
        item.Quantity, item.Unit, item.CostPerUnit,
        item.Quantity * item.CostPerUnit,
        item.PurchaseDate, item.ExpiryDate, item.ExpiryReminderDays,
        (int)(item.ExpiryDate - now).TotalDays,
        item.Status, item.CreatedAt
    );
}
