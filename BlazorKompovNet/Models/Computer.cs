namespace BlazorKompovNet.Models;

public sealed class Computer
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public Club? Club { get; set; }

    public string Number { get; set; } = string.Empty;

    public string? Name { get; set; }

    public int ComputerStatusId { get; set; }

    public ComputerStatus? Status { get; set; }

    public int ZoneId { get; set; }

    public ComputerZone? Zone { get; set; }

    public string? Processor { get; set; }

    public string? GraphicsCard { get; set; }

    public int RamGb { get; set; }

    public string? Monitor { get; set; }
}
