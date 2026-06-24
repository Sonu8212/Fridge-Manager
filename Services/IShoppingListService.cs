using ErrorOr;
using FridgeManager.Api.DTOs;
using FridgeManager.Api.Models;

namespace FridgeManager.Api.Services;

public interface IShoppingListService
{
    Task<List<ShoppingItemResponseDto>> GetAllAsync(string userId, CancellationToken ct = default);
    Task<ShoppingItemResponseDto> AddAsync(string userId, CreateShoppingItemDto dto, CancellationToken ct = default);
    Task AutoAddAsync(string userId, FridgeItem item, CancellationToken ct = default);
    Task<ErrorOr<Updated>> MarkPurchasedAsync(string userId, int id, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeleteAsync(string userId, int id, CancellationToken ct = default);
}
