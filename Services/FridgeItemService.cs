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
    public async Task<PagedResult<FridgeItemResponseDto>> GetAllAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await repository.GetPagedAsync(userId, page, pageSize, ct);
        return new PagedResult<FridgeItemResponseDto>(items.Select(x => MapToResponse(x, dateTime.UtcNow)).ToList(), total, page, pageSize);
    }

    public async Task<ErrorOr<FridgeItemResponseDto>> GetByIdAsync(string userId, int id, CancellationToken ct = default)
    {
        var item = await repository.GetByIdAsync(id, userId, ct);
        return item is null ? Errors.FridgeItem.NotFound(id) : MapToResponse(item, dateTime.UtcNow);
    }

    public async Task<FridgeItemResponseDto> CreateAsync(string userId, CreateFridgeItemDto dto, CancellationToken ct = default)
    {
        var item = new FridgeItem
        {
            UserId = userId,
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
        logger.LogInformation("User {UserId} created item {ItemId} ({Name})", userId, item.Id, item.Name);
        return MapToResponse(item, dateTime.UtcNow);
    }

    public async Task<ErrorOr<FridgeItemResponseDto>> UpdateAsync(string userId, int id, UpdateFridgeItemDto dto, CancellationToken ct = default)
    {
        var item = await repository.GetByIdAsync(id, userId, ct);
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
        return MapToResponse(item, dateTime.UtcNow);
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(string userId, int id, CancellationToken ct = default)
    {
        var item = await repository.GetByIdAsync(id, userId, ct);
        if (item is null) return Errors.FridgeItem.NotFound(id);
        await repository.DeleteAsync(item, ct);
        logger.LogInformation("User {UserId} deleted item {ItemId}", userId, id);
        return Result.Deleted;
    }

    public async Task<ErrorOr<FridgeItemResponseDto>> MarkAsUsedAsync(string userId, int id, MarkUsedDto dto, CancellationToken ct = default)
    {
        var item = await repository.GetByIdAsync(id, userId, ct);
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
            await shoppingList.AutoAddAsync(userId, item, ct);
            logger.LogInformation("Item {ItemId} fully consumed by user {UserId}. Auto-added to shopping list.", id, userId);
        }

        await repository.SaveChangesAsync(ct);
        return MapToResponse(item, dateTime.UtcNow);
    }

    public async Task<WastageReportDto> GetWastageReportAsync(string userId, int month, int year, CancellationToken ct = default)
    {
        var items = await repository.GetWastedInMonthAsync(userId, month, year, ct);
        return new WastageReportDto(
            month, year,
            items.Count(x => x.Status == ItemStatus.Expired),
            items.Count(x => x.Status == ItemStatus.Wasted),
            items.Sum(x => x.CostPerUnit * x.Quantity),
            items.Select(x => new WastageItemDto(x.Name, x.Category, x.Quantity, x.Unit, x.CostPerUnit * x.Quantity, x.ExpiryDate)).ToList()
        );
    }

    public async Task<List<ForecastDto>> GetForecastAsync(string userId, CancellationToken ct = default)
    {
        var thirtyDaysAgo = dateTime.UtcNow.AddDays(-30);

        var forecast = await db.ConsumptionLogs
            .Where(x => x.FridgeItem.UserId == userId && x.UsedAt >= thirtyDaysAgo)
            .GroupBy(x => new { x.FridgeItem.Name, x.FridgeItem.Category, x.FridgeItem.Unit, x.FridgeItem.CostPerUnit })
            .Select(g => new { g.Key.Name, g.Key.Category, g.Key.Unit, g.Key.CostPerUnit, TotalUsed = g.Sum(x => x.QuantityUsed) })
            .ToListAsync(ct);

        return forecast.Select(g =>
        {
            var weekly = g.TotalUsed / 4m;
            return new ForecastDto(g.Name, g.Category, g.Unit, weekly, g.TotalUsed,
                Math.Ceiling(weekly * 1.1m), Math.Ceiling(g.TotalUsed * 1.1m),
                Math.Round(weekly * 1.1m * g.CostPerUnit, 2),
                Math.Round(g.TotalUsed * 1.1m * g.CostPerUnit, 2));
        }).ToList();
    }

    public async Task<List<FridgeItemResponseDto>> GetExpiringItemsAsync(string userId, int withinDays = 7, CancellationToken ct = default)
    {
        var items = await repository.GetExpiringAsync(userId, withinDays, ct);
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
