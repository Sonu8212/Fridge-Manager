using System.Security.Claims;
using Asp.Versioning;
using FridgeManager.Api.DTOs;
using FridgeManager.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridgeManager.Api.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/fridge-items")]
public class FridgeItemsController(IFridgeItemService service, IRecipeService recipeService) : ApiController
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Page must be >= 1 and pageSize must be between 1 and 100.");
        return Ok(await service.GetAllAsync(UserId, page, pageSize, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        var result = await service.GetByIdAsync(UserId, id, ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiring([FromQuery] int withinDays = 7, CancellationToken ct = default)
        => Ok(await service.GetExpiringItemsAsync(UserId, withinDays, ct));

    [HttpGet("expiring/recipes")]
    public async Task<IActionResult> GetRecipesForExpiring([FromQuery] int withinDays = 7, CancellationToken ct = default)
    {
        var expiring = await service.GetExpiringItemsAsync(UserId, withinDays, ct);
        var recipes = await recipeService.SuggestRecipesAsync(expiring.Select(x => x.Name).ToList());
        return Ok(recipes);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateFridgeItemDto dto, CancellationToken ct = default)
    {
        var item = await service.CreateAsync(UserId, dto, ct);
        return CreatedAtAction(nameof(GetById), new { version = "1.0", id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateFridgeItemDto dto, CancellationToken ct = default)
    {
        var result = await service.UpdateAsync(UserId, id, dto, ct);
        return result.Match(Ok, Problem);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var result = await service.DeleteAsync(UserId, id, ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:int}/mark-used")]
    public async Task<IActionResult> MarkUsed(int id, MarkUsedDto dto, CancellationToken ct = default)
    {
        var result = await service.MarkAsUsedAsync(UserId, id, dto, ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("reports/wastage")]
    public async Task<IActionResult> GetWastageReport([FromQuery] int month, [FromQuery] int year, CancellationToken ct = default)
    {
        if (month < 1 || month > 12) return BadRequest("Month must be between 1 and 12.");
        if (year < 2000 || year > DateTime.UtcNow.Year) return BadRequest("Invalid year.");
        return Ok(await service.GetWastageReportAsync(UserId, month, year, ct));
    }

    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecast(CancellationToken ct = default)
        => Ok(await service.GetForecastAsync(UserId, ct));
}
