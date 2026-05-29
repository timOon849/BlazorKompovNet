using System.Security.Claims;
using BlazorKompovNet.Models;
using BlazorKompovNet.Services.Api;

namespace BlazorKompovNet.Services;

public sealed class ApiDashboardService(KompovApiClient api) : IDashboardService
{
    public async Task<DashboardStats> GetDashboardStatsAsync(ClaimsPrincipal user)
    {
        var stats = await api.GetAsync<ApiDashboardStats>(api.Admin("dashboard"));
        if (stats is null)
        {
            return new DashboardStats();
        }

        var currentClub = stats.CurrentClub is null ? null : ApiMapper.ToClub(stats.CurrentClub);
        return new DashboardStats
        {
            CurrentClub = currentClub,
            AvailableClubs = stats.AvailableClubs.Select(ApiMapper.ToClub).ToList(),
            RevenueToday = stats.RevenueToday,
            RevenueCurrentShift = stats.RevenueCurrentShift,
            VisitorsToday = stats.VisitorsToday,
            ActiveVisitors = stats.ActiveVisitors,
            AvailableComputers = stats.AvailableComputers,
            BusyComputers = stats.BusyComputers,
            ReservedComputers = stats.ReservedComputers,
            MaintenanceComputers = stats.MaintenanceComputers,
            DisabledComputers = stats.DisabledComputers,
            BookingsToday = stats.BookingsToday,
            ActiveSessionsCount = stats.ActiveSessionsCount,
            CompletedSessionsToday = stats.CompletedSessionsToday,
            TopUpsToday = stats.TopUpsToday,
            TopUpAmountToday = stats.TopUpAmountToday,
            CurrentShift = stats.CurrentShift is null
                ? null
                : ApiMapper.ToShift(stats.CurrentShift, currentClub)
        };
    }
}
