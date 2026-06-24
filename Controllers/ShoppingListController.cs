using Asp.Versioning;
using FridgeManager.Api.DTOs;
using FridgeManager.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FridgeManager.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shopping-list")]
public class ShoppingListController(IShoppingListService service) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
        => Ok(await service.GetAllAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Add(CreateShoppingItemDto dto, CancellationToken ct = default)
        => Ok(await service.AddAsync(dto, ct));

    [HttpPost("{id:int}/purchased")]
    public async Task<IActionResult> MarkPurchased(int id, CancellationToken ct = default)
    {
        var result = await service.MarkPurchasedAsync(id, ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var result = await service.DeleteAsync(id, ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
