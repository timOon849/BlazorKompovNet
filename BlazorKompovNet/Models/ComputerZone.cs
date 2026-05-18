namespace BlazorKompovNet.Models;

public sealed class ComputerZone
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public Club? Club { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<Computer> Computers { get; set; } = [];
}
