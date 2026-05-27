namespace BlazorKompovNet.Models;

public sealed class DashboardStats
{
    public Club? CurrentClub { get; set; }

    public IReadOnlyList<Club> AvailableClubs { get; set; } = [];

    public decimal RevenueToday { get; set; }

    public decimal RevenueCurrentShift { get; set; }

    public int VisitorsToday { get; set; }

    public int ActiveVisitors { get; set; }

    public int AvailableComputers { get; set; }

    public int BusyComputers { get; set; }

    public int ReservedComputers { get; set; }

    public int MaintenanceComputers { get; set; }

    public int DisabledComputers { get; set; }

    public int BookingsToday { get; set; }

    public int ActiveSessionsCount { get; set; }

    public int CompletedSessionsToday { get; set; }

    public int TopUpsToday { get; set; }

    public decimal TopUpAmountToday { get; set; }

    public CashierShift? CurrentShift { get; set; }
}
