namespace BlazorKompovNet.Models;

public static class ModelLabels
{
    public static string GetSessionDisplayKey(GameSession session)
    {
        return session.Status == GameSessionStatus.Active && session.PlannedEndAt < DateTime.Now
            ? "Overdue"
            : session.Status.ToString();
    }

    public static bool IsSessionActive(GameSession session) =>
        session.Status == GameSessionStatus.Active;

    public static string GetSessionStatusText(GameSession session)
    {
        return GetSessionDisplayKey(session) switch
        {
            "Active" => "Активна",
            "Overdue" => "Просрочена",
            "Completed" => "Завершена",
            _ => session.Status.ToString()
        };
    }

    public static string GetSessionStatusBadgeClass(string displayKey)
    {
        return displayKey switch
        {
            "Active" => "text-bg-primary",
            "Overdue" => "text-bg-warning",
            "Completed" => "text-bg-success",
            _ => "text-bg-secondary"
        };
    }

    public static string GetTransactionTypeText(string type)
    {
        return type switch
        {
            TransactionType.BalanceTopUp => "Пополнение баланса",
            TransactionType.BonusAccrual => "Бонусное пополнение",
            TransactionType.SessionStart => "Старт сессии",
            TransactionType.SessionExtension => "Продление сессии",
            _ => type
        };
    }

    public static bool IsSessionCharge(string type)
    {
        return type == TransactionType.SessionStart || type == TransactionType.SessionExtension;
    }

    public static string FormatTransactionAmount(Transaction transaction)
    {
        var prefix = IsSessionCharge(transaction.Type) ? "-" : "+";
        return $"{prefix}{transaction.Amount:N0} ₽";
    }

    public static string GetPaymentStatusText(string status)
    {
        return status switch
        {
            PaymentStatus.Pending => "Ожидает",
            PaymentStatus.Paid => "Оплачено",
            PaymentStatus.Refunded => "Возврат",
            PaymentStatus.Cancelled => "Отменено",
            _ => status
        };
    }

    public static string GetPaymentStatusBadgeClass(string status)
    {
        return status switch
        {
            PaymentStatus.Paid => "text-bg-success",
            PaymentStatus.Pending => "text-bg-warning",
            PaymentStatus.Refunded => "text-bg-info",
            PaymentStatus.Cancelled => "text-bg-secondary",
            _ => "text-bg-light"
        };
    }

    public static string GetBookingStatusText(string status)
    {
        return status switch
        {
            BookingStatus.Created => "Создана",
            BookingStatus.Active => "Активна",
            BookingStatus.Completed => "Завершена",
            BookingStatus.Cancelled => "Отменена",
            BookingStatus.NoShow => "Неявка",
            _ => status
        };
    }

    public static string GetBookingStatusBadgeClass(string status)
    {
        return status switch
        {
            BookingStatus.Created => "text-bg-warning",
            BookingStatus.Active => "text-bg-primary",
            BookingStatus.Completed => "text-bg-success",
            BookingStatus.Cancelled => "text-bg-secondary",
            BookingStatus.NoShow => "text-bg-danger",
            _ => "text-bg-light"
        };
    }
}
