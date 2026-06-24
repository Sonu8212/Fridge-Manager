using ErrorOr;
using FridgeManager.Api.DTOs;
using FridgeManager.Api.Models;

namespace FridgeManager.Api.Services;

public interface IShoppingListService
{
    Task<List<ShoppingItemResponseDto>> GetAllAsync(CancellationToken ct = default);
    Task<ShoppingItemResponseDto> AddAsync(CreateShoppingItemDto dto, CancellationToken ct = default);
    Task AutoAddAsync(FridgeItem item, CancellationToken ct = default);
    Task<ErrorOr<Updated>> MarkPurchasedAsync(int id, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken ct = default);
}
