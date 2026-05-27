namespace BlazorKompovNet.Models;

public sealed class SessionExtension
{
    public int Id { get; set; }

    public int GameSessionId { get; set; }

    public GameSession? GameSession { get; set; }

    public int? TariffId { get; set; }

    public Tariff? Tariff { get; set; }

    public int AddedMinutes { get; set; }

    public decimal Amount { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
