using FridgeManager.Api.DTOs;
using FridgeManager.Api.Models;

namespace FridgeManager.Api.Services;

public interface IShoppingListService
{
    Task<List<ShoppingItemResponseDto>> GetAllAsync();
    Task<ShoppingItemResponseDto> AddAsync(CreateShoppingItemDto dto);
    Task AutoAddAsync(FridgeItem item);
    Task<bool> MarkPurchasedAsync(int id);
    Task<bool> DeleteAsync(int id);
}
