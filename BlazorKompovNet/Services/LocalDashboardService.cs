using System.Security.Claims;
using BlazorKompovNet.Models;

namespace BlazorKompovNet.Services;

public sealed class LocalDashboardService : IDashboardService
{
    private readonly IClubManagementService clubManagementService;
    private readonly IReadOnlyList<Club> clubs =
    [
        new() { Id = 1, Name = "KompovNet", Address = "ул. Ленина, 15" },
        new() { Id = 2, Name = "KompovNet Север", Address = "пр. Мира, 44" },
        new() { Id = 3, Name = "KompovNet Bootcamp", Address = "ул. Турнирная, 7" }
    ];

    public LocalDashboardService(IClubManagementService clubManagementService)
    {
        this.clubManagementService = clubManagementService;
    }

    public async Task<DashboardStats> GetDashboardStatsAsync(ClaimsPrincipal user)
    {
        var currentClub = clubs[0];
        var currentShift = await clubManagementService.GetCurrentShiftAsync();
        var zones = await clubManagementService.GetZonesAsync();
        var transactions = await clubManagementService.GetTransactionsAsync();
        var sessions = await clubManagementService.GetSessionsAsync();
        var activeBookings = await clubManagementService.GetActiveBookingsAsync();
        var today = DateTime.Today;
        var computers = zones.SelectMany(zone => zone.Computers).ToList();
        var paidTransactions = transactions.Where(transaction => transaction.Status == PaymentStatus.Paid).ToList();

        var stats = new DashboardStats
        {
            CurrentClub = currentClub,
            AvailableClubs = clubs,
            RevenueToday = paidTransactions
                .Where(transaction => transaction.CreatedAt.ToLocalTime().Date == today)
                .Sum(transaction => transaction.Amount),
            RevenueCurrentShift = currentShift is null
                ? 0
                : paidTransactions
                    .Where(transaction => transaction.CashierShiftId == currentShift.Id)
                    .Sum(transaction => transaction.Amount),
            VisitorsToday = sessions
                .Where(session => session.StartedAt.Date == today)
                .Select(session => session.ClientId)
                .Distinct()
                .Count(),
            ActiveVisitors = sessions
                .Where(session => session.Status == "Active")
                .Select(session => session.ClientId)
                .Distinct()
                .Count(),
            AvailableComputers = computers.Count(computer => computer.Status?.Code == ComputerStatusCodes.Available),
            BusyComputers = computers.Count(computer => computer.Status?.Code == ComputerStatusCodes.Busy),
            ReservedComputers = computers.Count(computer => computer.Status?.Code == ComputerStatusCodes.Reserved),
            MaintenanceComputers = computers.Count(computer => computer.Status?.Code == ComputerStatusCodes.Maintenance),
            BookingsToday = activeBookings.Count(booking => booking.StartsAt.Date == today),
            CurrentShift = currentShift
        };

        return stats;
    }
}
