using ErrorOr;
using FridgeManager.Api.Common;
using FridgeManager.Api.DTOs;

namespace FridgeManager.Api.Services;

public interface IFridgeItemService
{
    Task<PagedResult<FridgeItemResponseDto>> GetAllAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    Task<ErrorOr<FridgeItemResponseDto>> GetByIdAsync(string userId, int id, CancellationToken ct = default);
    Task<FridgeItemResponseDto> CreateAsync(string userId, CreateFridgeItemDto dto, CancellationToken ct = default);
    Task<ErrorOr<FridgeItemResponseDto>> UpdateAsync(string userId, int id, UpdateFridgeItemDto dto, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeleteAsync(string userId, int id, CancellationToken ct = default);
    Task<ErrorOr<FridgeItemResponseDto>> MarkAsUsedAsync(string userId, int id, MarkUsedDto dto, CancellationToken ct = default);
    Task<WastageReportDto> GetWastageReportAsync(string userId, int month, int year, CancellationToken ct = default);
    Task<List<ForecastDto>> GetForecastAsync(string userId, CancellationToken ct = default);
    Task<List<FridgeItemResponseDto>> GetExpiringItemsAsync(string userId, int withinDays = 7, CancellationToken ct = default);
}
