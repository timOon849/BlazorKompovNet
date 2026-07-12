using System.Security.Claims;
using BlazorKompovNet.Models;

namespace BlazorKompovNet.Services;

public interface IDashboardService
{
    Task<DashboardStats> GetDashboardStatsAsync(ClaimsPrincipal user);

    Task<DashboardAnalytics> GetDashboardAnalyticsAsync(ClaimsPrincipal user, int days = 7);
}
