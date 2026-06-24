using FridgeManager.Api.Models;

namespace FridgeManager.Api.Repositories;

public interface IShoppingListRepository
{
    Task<List<ShoppingListItem>> GetPendingAsync(string userId, CancellationToken ct = default);
    Task<ShoppingListItem?> GetByIdAsync(int id, string userId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string userId, string name, CancellationToken ct = default);
    Task AddAsync(ShoppingListItem item, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task DeleteAsync(ShoppingListItem item, CancellationToken ct = default);
}
