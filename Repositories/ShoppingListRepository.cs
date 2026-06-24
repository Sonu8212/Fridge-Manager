using FridgeManager.Api.Data;
using FridgeManager.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FridgeManager.Api.Repositories;

public class ShoppingListRepository(AppDbContext db) : IShoppingListRepository
{
    public Task<List<ShoppingListItem>> GetPendingAsync(CancellationToken ct = default) =>
        db.ShoppingListItems.Where(x => !x.IsPurchased).OrderBy(x => x.CreatedAt).ToListAsync(ct);

    public Task<ShoppingListItem?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.ShoppingListItems.FindAsync([id], ct).AsTask();

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default) =>
        db.ShoppingListItems.AnyAsync(x => x.Name == name && !x.IsPurchased, ct);

    public async Task AddAsync(ShoppingListItem item, CancellationToken ct = default)
    {
        db.ShoppingListItems.Add(item);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task DeleteAsync(ShoppingListItem item, CancellationToken ct = default)
    {
        db.ShoppingListItems.Remove(item);
        await db.SaveChangesAsync(ct);
    }
}
