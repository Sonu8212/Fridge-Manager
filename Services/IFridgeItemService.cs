using ErrorOr;
using FridgeManager.Api.Common;
using FridgeManager.Api.DTOs;

namespace FridgeManager.Api.Services;

public interface IFridgeItemService
{
    Task<PagedResult<FridgeItemResponseDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<ErrorOr<FridgeItemResponseDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<FridgeItemResponseDto> CreateAsync(CreateFridgeItemDto dto, CancellationToken ct = default);
    Task<ErrorOr<FridgeItemResponseDto>> UpdateAsync(int id, UpdateFridgeItemDto dto, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken ct = default);
    Task<ErrorOr<FridgeItemResponseDto>> MarkAsUsedAsync(int id, MarkUsedDto dto, CancellationToken ct = default);
    Task<WastageReportDto> GetWastageReportAsync(int month, int year, CancellationToken ct = default);
    Task<List<ForecastDto>> GetForecastAsync(CancellationToken ct = default);
    Task<List<FridgeItemResponseDto>> GetExpiringItemsAsync(int withinDays = 7, CancellationToken ct = default);
}
