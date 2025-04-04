using Hangfire.Dashboard;

namespace ClimbingBookerApi.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // TODO: Implement proper authorization logic
        // For development, allow all access
        return true;
    }
} 