namespace BlazorKompovNet.Models;

public sealed class Tariff
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TimeSpan Duration { get; set; }

    public bool IsHourly { get; set; }

    public bool IsActive { get; set; } = true;

    public List<TariffZone> TariffZones { get; set; } = [];
}
