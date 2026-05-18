namespace BlazorKompovNet.Models;

public sealed class ComputerStatus
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool CanStartSession { get; set; }
}

public static class ComputerStatusCodes
{
    public const string Available = "Available";
    public const string Busy = "Busy";
    public const string Reserved = "Reserved";
    public const string Maintenance = "Maintenance";
    public const string Disabled = "Disabled";
}
