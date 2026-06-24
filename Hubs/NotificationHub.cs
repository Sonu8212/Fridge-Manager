using Microsoft.AspNetCore.SignalR;

namespace FridgeManager.Api.Hubs;

public class NotificationHub : Hub
{
    public async Task JoinUser(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
    }
}
