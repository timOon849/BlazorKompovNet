namespace BlazorKompovNet.Models;

public sealed class DashboardAnalytics
{
    public int Days { get; set; }

    public IReadOnlyList<DailyMetricPoint> RevenueByDay { get; set; } = [];

    public IReadOnlyList<DailyMetricPoint> SessionsByDay { get; set; } = [];

    public IReadOnlyList<NamedAmountPoint> RevenueByPaymentType { get; set; } = [];

    public IReadOnlyList<NamedCountPoint> ComputerStatus { get; set; } = [];
}

public sealed class DailyMetricPoint
{
    public string Label { get; set; } = "";

    public decimal Amount { get; set; }

    public int Count { get; set; }
}

public sealed class NamedAmountPoint
{
    public string Name { get; set; } = "";

    public decimal Amount { get; set; }
}

public sealed class NamedCountPoint
{
    public string Name { get; set; } = "";

    public int Count { get; set; }
}
