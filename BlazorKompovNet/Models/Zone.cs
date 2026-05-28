namespace BlazorKompovNet.Models;

public sealed class Zone
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CPU { get; set; } = string.Empty;
    public string GPU { get; set; } = string.Empty;
    public string RAM { get; set; } = string.Empty;
    public string MonitorResolution { get; set; } = string.Empty;
    public string MonitorHz { get; set; } = string.Empty;
    public string MonitorSize { get; set; } = string.Empty;
}
