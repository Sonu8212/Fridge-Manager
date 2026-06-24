using FridgeManager.Api.DTOs;
using FridgeManager.Api.Models;

namespace FridgeManager.Api.Services;

public interface IFridgeItemService
{
    Task<List<FridgeItemResponseDto>> GetAllAsync();
    Task<FridgeItemResponseDto?> GetByIdAsync(int id);
    Task<FridgeItemResponseDto> CreateAsync(CreateFridgeItemDto dto);
    Task<FridgeItemResponseDto?> UpdateAsync(int id, UpdateFridgeItemDto dto);
    Task<bool> DeleteAsync(int id);
    Task<FridgeItemResponseDto?> MarkAsUsedAsync(int id, MarkUsedDto dto);
    Task<WastageReportDto> GetWastageReportAsync(int month, int year);
    Task<List<ForecastDto>> GetForecastAsync();
    Task<List<FridgeItemResponseDto>> GetExpiringItemsAsync(int withinDays = 7);
}
