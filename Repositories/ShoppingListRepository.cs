using FridgeManager.Api.Data;
using FridgeManager.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FridgeManager.Api.Repositories;

public class ShoppingListRepository(AppDbContext db) : IShoppingListRepository
{
    public Task<List<ShoppingListItem>> GetPendingAsync(string userId, CancellationToken ct = default) =>
        db.ShoppingListItems.Where(x => x.UserId == userId && !x.IsPurchased).OrderBy(x => x.CreatedAt).ToListAsync(ct);

    public Task<ShoppingListItem?> GetByIdAsync(int id, string userId, CancellationToken ct = default) =>
        db.ShoppingListItems.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);

    public Task<bool> ExistsByNameAsync(string userId, string name, CancellationToken ct = default) =>
        db.ShoppingListItems.AnyAsync(x => x.UserId == userId && x.Name == name && !x.IsPurchased, ct);

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
