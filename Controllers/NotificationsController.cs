using FridgeManager.Api.Data;
using FridgeManager.Api.Jobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FridgeManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController(AppDbContext db, ExpiryCheckJob expiryJob) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var notifications = await db.Notifications
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToListAsync();
        return Ok(notifications);
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var n = await db.Notifications.FindAsync(id);
        if (n is null) return NotFound();
        n.IsRead = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // manual trigger for testing
    [HttpPost("check-expiry")]
    public async Task<IActionResult> TriggerExpiryCheck()
    {
        await expiryJob.RunAsync();
        return Ok("Expiry check completed.");
    }
}
