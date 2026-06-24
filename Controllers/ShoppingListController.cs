using FridgeManager.Api.DTOs;
using FridgeManager.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FridgeManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShoppingListController(IShoppingListService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await service.GetAllAsync());

    [HttpPost]
    public async Task<IActionResult> Add(CreateShoppingItemDto dto)
        => Ok(await service.AddAsync(dto));

    [HttpPost("{id}/purchased")]
    public async Task<IActionResult> MarkPurchased(int id)
        => await service.MarkPurchasedAsync(id) ? NoContent() : NotFound();

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteAsync(id) ? NoContent() : NotFound();
}
