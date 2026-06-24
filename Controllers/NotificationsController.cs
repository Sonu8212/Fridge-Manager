using System.Security.Claims;
using Asp.Versioning;
using FridgeManager.Api.Data;
using FridgeManager.Api.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FridgeManager.Api.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
public class NotificationsController(AppDbContext db, ExpiryCheckJob expiryJob) : ApiController
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var notifications = await db.Notifications
            .Where(x => x.UserId == UserId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToListAsync(ct);
        return Ok(notifications);
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct = default)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId, ct);
        if (n is null) return NotFound();
        n.IsRead = true;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // Manual trigger — useful for testing without waiting for the scheduled job
    [HttpPost("check-expiry")]
    public async Task<IActionResult> TriggerExpiryCheck(CancellationToken ct = default)
    {
        await expiryJob.RunAsync(ct);
        return Ok(new { message = "Expiry check completed." });
    }
}
