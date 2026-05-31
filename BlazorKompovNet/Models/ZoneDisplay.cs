namespace BlazorKompovNet.Models;

public static class ZoneDisplay
{
    public static string GetHardwareLine(ComputerZone zone)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(zone.CPU))
        {
            parts.Add(zone.CPU.Trim());
        }

        if (!string.IsNullOrWhiteSpace(zone.GPU))
        {
            parts.Add(zone.GPU.Trim());
        }

        if (!string.IsNullOrWhiteSpace(zone.RAM))
        {
            parts.Add(zone.RAM.Trim());
        }

        return parts.Count > 0 ? string.Join(" · ", parts) : string.Empty;
    }

    public static string GetMonitorLine(ComputerZone zone)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(zone.MonitorSize))
        {
            parts.Add(zone.MonitorSize.Trim());
        }

        if (!string.IsNullOrWhiteSpace(zone.MonitorResolution))
        {
            parts.Add(zone.MonitorResolution.Trim());
        }

        if (!string.IsNullOrWhiteSpace(zone.MonitorHz))
        {
            parts.Add($"{zone.MonitorHz.Trim()} Гц");
        }

        return parts.Count > 0 ? string.Join(" · ", parts) : string.Empty;
    }

    public static bool HasHardwareInfo(ComputerZone zone) =>
        !string.IsNullOrWhiteSpace(GetHardwareLine(zone))
        || !string.IsNullOrWhiteSpace(GetMonitorLine(zone));
}
