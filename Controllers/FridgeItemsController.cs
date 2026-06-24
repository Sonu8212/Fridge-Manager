using FridgeManager.Api.DTOs;
using FridgeManager.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FridgeManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FridgeItemsController(IFridgeItemService service, IRecipeService recipeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiring([FromQuery] int withinDays = 7)
        => Ok(await service.GetExpiringItemsAsync(withinDays));

    [HttpGet("expiring/recipes")]
    public async Task<IActionResult> GetRecipesForExpiring([FromQuery] int withinDays = 7)
    {
        var expiring = await service.GetExpiringItemsAsync(withinDays);
        var ingredients = expiring.Select(x => x.Name).ToList();
        var recipes = await recipeService.SuggestRecipesAsync(ingredients);
        return Ok(recipes);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateFridgeItemDto dto)
    {
        var item = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateFridgeItemDto dto)
    {
        var item = await service.UpdateAsync(id, dto);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteAsync(id) ? NoContent() : NotFound();

    [HttpPost("{id}/mark-used")]
    public async Task<IActionResult> MarkUsed(int id, MarkUsedDto dto)
    {
        var item = await service.MarkAsUsedAsync(id, dto);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("report/wastage")]
    public async Task<IActionResult> GetWastageReport([FromQuery] int month, [FromQuery] int year)
    {
        if (month < 1 || month > 12) return BadRequest("Month must be between 1 and 12.");
        return Ok(await service.GetWastageReportAsync(month, year));
    }

    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecast()
        => Ok(await service.GetForecastAsync());
}
