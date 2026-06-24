using FridgeManager.Api.Data;
using FridgeManager.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FridgeManager.Api.Repositories;

public class FridgeItemRepository(AppDbContext db) : IFridgeItemRepository
{
    public Task<FridgeItem?> GetByIdAsync(int id, string userId, CancellationToken ct = default) =>
        db.FridgeItems.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);

    public async Task<(List<FridgeItem> Items, int TotalCount)> GetPagedAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.FridgeItems
            .Where(x => x.UserId == userId && x.Status == ItemStatus.Active)
            .OrderBy(x => x.ExpiryDate);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<List<FridgeItem>> GetExpiringAsync(string userId, int withinDays, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(withinDays);
        return db.FridgeItems
            .Where(x => x.UserId == userId && x.Status == ItemStatus.Active && x.ExpiryDate <= cutoff)
            .OrderBy(x => x.ExpiryDate)
            .ToListAsync(ct);
    }

    public Task<List<FridgeItem>> GetActiveByUserAsync(string userId, CancellationToken ct = default) =>
        db.FridgeItems.Where(x => x.UserId == userId && x.Status == ItemStatus.Active).ToListAsync(ct);

    public async Task<List<(string UserId, List<FridgeItem> Items)>> GetAllActiveGroupedByUserAsync(CancellationToken ct = default)
    {
        var all = await db.FridgeItems
            .Where(x => x.Status == ItemStatus.Active)
            .ToListAsync(ct);

        return all.GroupBy(x => x.UserId)
                  .Select(g => (g.Key, g.ToList()))
                  .ToList();
    }

    public Task<List<FridgeItem>> GetWastedInMonthAsync(string userId, int month, int year, CancellationToken ct = default)
    {
        return db.FridgeItems
            .Where(x => x.UserId == userId
                     && (x.Status == ItemStatus.Expired || x.Status == ItemStatus.Wasted)
                     && x.ExpiryDate.Month == month && x.ExpiryDate.Year == year)
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
