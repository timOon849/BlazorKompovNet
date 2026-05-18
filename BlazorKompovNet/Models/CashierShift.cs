namespace BlazorKompovNet.Models;

public sealed class CashierShift
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public Club? Club { get; set; }

    public int CashierId { get; set; }

    public Cashier? Cashier { get; set; }

    public DateTime OpenedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public decimal OpeningCashAmount { get; set; }

    public decimal CurrentCashAmount { get; set; }

    public bool IsOpen => ClosedAt is null;
}
