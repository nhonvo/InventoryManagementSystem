using Hangfire.Dashboard;

namespace InventoryAlert.Api.Filters;

public class DevDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}
