using System.Security.Claims;
using BlazorKompovNet.Models;

namespace BlazorKompovNet.Services;

public interface IDashboardService
{
    Task<DashboardStats> GetDashboardStatsAsync(ClaimsPrincipal user);
}
