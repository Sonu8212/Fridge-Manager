using FridgeManager.Api.Models;

namespace FridgeManager.Api.Repositories;

public interface IFridgeItemRepository
{
    Task<FridgeItem?> GetByIdAsync(int id, string userId, CancellationToken ct = default);
    Task<(List<FridgeItem> Items, int TotalCount)> GetPagedAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    Task<List<FridgeItem>> GetExpiringAsync(string userId, int withinDays, CancellationToken ct = default);
    Task<List<FridgeItem>> GetActiveByUserAsync(string userId, CancellationToken ct = default);

    // Used by the expiry job — returns all active items grouped by userId
    Task<List<(string UserId, List<FridgeItem> Items)>> GetAllActiveGroupedByUserAsync(CancellationToken ct = default);

    Task<List<FridgeItem>> GetWastedInMonthAsync(string userId, int month, int year, CancellationToken ct = default);
    Task AddAsync(FridgeItem item, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task DeleteAsync(FridgeItem item, CancellationToken ct = default);
}
