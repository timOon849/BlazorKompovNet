namespace BlazorKompovNet.Models;

public sealed class Transaction
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public Club? Club { get; set; }

    public int? CashierShiftId { get; set; }

    public CashierShift? CashierShift { get; set; }

    public int? ClientId { get; set; }

    public Client? Client { get; set; }

    public int? GameSessionId { get; set; }

    public GameSession? GameSession { get; set; }

    public int PaymentTypeId { get; set; }

    public PaymentType? PaymentType { get; set; }

    public decimal Amount { get; set; }

    public string Type { get; set; } = TransactionType.BalanceTopUp;

    public string Status { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
