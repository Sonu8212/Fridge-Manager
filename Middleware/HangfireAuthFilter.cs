using Hangfire.Dashboard;

namespace FridgeManager.Api.Middleware;

public class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        // In dev allow all; in production wire up real auth here
        return httpContext.Request.Host.Host == "localhost";
    }
}
