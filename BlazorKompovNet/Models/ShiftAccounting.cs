namespace BlazorKompovNet.Models;

public static class ShiftAccounting
{
    public static decimal GetShiftRevenue(IEnumerable<Transaction> transactions, int shiftId) =>
        transactions
            .Where(transaction => transaction.CashierShiftId == shiftId)
            .Where(ModelLabels.CountsTowardRevenue)
            .Sum(transaction => transaction.Amount);

    public static decimal GetTodayRevenue(IEnumerable<Transaction> transactions) =>
        transactions
            .Where(transaction => ClubDateTime.ClubDate(transaction.CreatedAt) == ClubDateTime.Today)
            .Where(ModelLabels.CountsTowardRevenue)
            .Sum(transaction => transaction.Amount);

    public static decimal GetExpectedCashAmount(CashierShift shift, IEnumerable<Transaction> transactions) =>
        shift.OpeningCashAmount + transactions
            .Where(transaction => transaction.CashierShiftId == shift.Id)
            .Where(transaction => transaction.Status == PaymentStatus.Paid)
            .Where(transaction => transaction.PaymentType?.Code == PaymentTypeCodes.Cash)
            .Sum(transaction => transaction.Amount);
}
