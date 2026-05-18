namespace BlazorKompovNet.Models;

public sealed class TariffZone
{
    public int Id { get; set; }

    public int TariffId { get; set; }

    public Tariff? Tariff { get; set; }

    public int ZoneId { get; set; }

    public ComputerZone? Zone { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;
}
