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
        var clubId = currentClub?.Id;
        var context = await api.GetAsync<ApiAdminPanelContext>(api.Admin("context"));
        var paymentTypes = context?.PaymentTypes ?? [];

        var transactions = (await api.GetListAsync<ApiTransaction>(api.Admin("transactions")))
            .Where(transaction => clubId is null || transaction.ClubId == clubId)
            .Select(transaction =>
            {
                var mapped = ApiMapper.ToTransaction(transaction, currentClub);
                var paymentType = paymentTypes.FirstOrDefault(type => type.Id == transaction.PaymentTypeId);
                if (paymentType is not null)
                {
                    mapped.PaymentType = ApiMapper.ToPaymentType(paymentType);
                }

                return mapped;
            })
            .ToList();

        var revenueToday = ShiftAccounting.GetTodayRevenue(transactions);
        var revenueCurrentShift = stats.CurrentShift is null
            ? 0
            : ShiftAccounting.GetShiftRevenue(transactions, stats.CurrentShift.Id);

        return new DashboardStats
        {
            CurrentClub = currentClub,
            AvailableClubs = stats.AvailableClubs.Select(ApiMapper.ToClub).ToList(),
            RevenueToday = revenueToday,
            RevenueCurrentShift = revenueCurrentShift,
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
