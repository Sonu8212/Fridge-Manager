using FridgeManager.Api.Models;

namespace FridgeManager.Api.Repositories;

public interface IFridgeItemRepository
{
    Task<FridgeItem?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(List<FridgeItem> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<List<FridgeItem>> GetExpiringAsync(int withinDays, CancellationToken ct = default);
    Task<List<FridgeItem>> GetActiveAsync(CancellationToken ct = default);
    Task<List<FridgeItem>> GetWastedInMonthAsync(int month, int year, CancellationToken ct = default);
    Task AddAsync(FridgeItem item, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task DeleteAsync(FridgeItem item, CancellationToken ct = default);
}
