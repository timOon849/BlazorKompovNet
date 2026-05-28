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
                .Where(transaction =>
                    transaction.CreatedAt.ToLocalTime().Date == today &&
                    !ModelLabels.IsSessionCharge(transaction.Type))
                .Sum(transaction => transaction.Amount),
            RevenueCurrentShift = currentShift is null
                ? 0
                : paidTransactions
                    .Where(transaction =>
                        transaction.CashierShiftId == currentShift.Id &&
                        !ModelLabels.IsSessionCharge(transaction.Type))
                    .Sum(transaction => transaction.Amount),
            VisitorsToday = sessions
                .Where(session => session.StartedAt.Date == today)
                .Select(session => session.ClientId)
                .Distinct()
                .Count(),
            ActiveVisitors = sessions
                .Where(session => session.Status == GameSessionStatus.Active)
                .Select(session => session.ClientId)
                .Distinct()
                .Count(),
            ActiveSessionsCount = sessions.Count(session => session.Status == GameSessionStatus.Active),
            CompletedSessionsToday = sessions.Count(session =>
                session.Status == GameSessionStatus.Completed &&
                (session.EndedAt?.Date == today || session.StartedAt.Date == today)),
            TopUpsToday = paidTransactions.Count(transaction =>
                transaction.CreatedAt.ToLocalTime().Date == today &&
                transaction.Type == TransactionType.BalanceTopUp || transaction.Type == TransactionType.BonusAccrual),
            TopUpAmountToday = paidTransactions
                .Where(transaction =>
                    transaction.CreatedAt.ToLocalTime().Date == today &&
                    transaction.Type == TransactionType.BalanceTopUp || transaction.Type == TransactionType.BonusAccrual)
                .Sum(transaction => transaction.Amount),
            AvailableComputers = computers.Count(computer => computer.Status?.Code == ComputerStatusCodes.Available),
            BusyComputers = computers.Count(computer => computer.Status?.Code == ComputerStatusCodes.Busy),
            ReservedComputers = computers.Count(computer => computer.Status?.Code == ComputerStatusCodes.Reserved),
            MaintenanceComputers = computers.Count(computer => computer.Status?.Code == ComputerStatusCodes.Maintenance),
            DisabledComputers = computers.Count(computer => computer.Status?.Code == ComputerStatusCodes.Disabled),
            BookingsToday = activeBookings.Count(booking => booking.StartsAt.Date == today),
            CurrentShift = currentShift
        };

        return stats;
    }
}
