namespace BlazorKompovNet.Models;

public sealed class GameSession
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public Club? Club { get; set; }

    public int ComputerId { get; set; }

    public Computer? Computer { get; set; }

    public int ClientId { get; set; }

    public Client? Client { get; set; }

    public int CashierShiftId { get; set; }

    public CashierShift? CashierShift { get; set; }

    public int TariffId { get; set; }

    public Tariff? Tariff { get; set; }

    public int TariffZoneId { get; set; }

    public TariffZone? TariffZone { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime PlannedEndAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public decimal InitialPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public GameSessionStatus Status { get; set; } = GameSessionStatus.Active;

    public List<Transaction> Transactions { get; set; } = [];

    public List<SessionExtension> Extensions { get; set; } = [];
}
