using FridgeManager.Api.Data;
using FridgeManager.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FridgeManager.Api.Repositories;

public class FridgeItemRepository(AppDbContext db) : IFridgeItemRepository
{
    public Task<FridgeItem?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.FridgeItems.FindAsync([id], ct).AsTask();

    public async Task<(List<FridgeItem> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.FridgeItems.Where(x => x.Status == ItemStatus.Active).OrderBy(x => x.ExpiryDate);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<List<FridgeItem>> GetExpiringAsync(int withinDays, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(withinDays);
        return db.FridgeItems
            .Where(x => x.Status == ItemStatus.Active && x.ExpiryDate <= cutoff)
            .OrderBy(x => x.ExpiryDate)
            .ToListAsync(ct);
    }

    public Task<List<FridgeItem>> GetActiveAsync(CancellationToken ct = default) =>
        db.FridgeItems.Where(x => x.Status == ItemStatus.Active).ToListAsync(ct);

    public Task<List<FridgeItem>> GetWastedInMonthAsync(int month, int year, CancellationToken ct = default)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        return db.FridgeItems
            .Where(x => (x.Status == ItemStatus.Expired || x.Status == ItemStatus.Wasted)
                     && x.ExpiryDate >= start && x.ExpiryDate < end)
            .ToListAsync(ct);
    }

    public async Task AddAsync(FridgeItem item, CancellationToken ct = default)
    {
        db.FridgeItems.Add(item);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task DeleteAsync(FridgeItem item, CancellationToken ct = default)
    {
        db.FridgeItems.Remove(item);
        await db.SaveChangesAsync(ct);
    }
}
