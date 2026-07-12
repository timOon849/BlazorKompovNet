using System.Text.RegularExpressions;

namespace BlazorKompovNet.Services;

public static partial class InputValidators
{
    [GeneratedRegex(@"^[^@]+@[^@]+\.[^@]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9._-]{3,32}$")]
    private static partial Regex LoginRegex();

    public static string? RequiredName(string? value, string label)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length < 2)
            return $"Укажите {label.ToLowerInvariant()} (минимум 2 символа).";
        return null;
    }

    public static string? OptionalPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = Regex.Replace(value, @"\D", string.Empty);
        return digits.Length is < 10 or > 11
            ? "Введите корректный номер телефона."
            : null;
    }

    public static string? OptionalEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return EmailRegex().IsMatch(value.Trim())
            ? null
            : "Введите корректный email.";
    }

    public static string? Login(string? value, bool required = false)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            return required ? "Укажите логин." : null;

        return LoginRegex().IsMatch(trimmed)
            ? null
            : "Логин: 3–32 символа, латиница, цифры, . _ -";
    }

    public static string? Password(string? value, bool required, int minLength = 3)
    {
        if (string.IsNullOrWhiteSpace(value))
            return required ? "Укажите пароль." : null;

        return value.Length < minLength
            ? $"Пароль должен быть не короче {minLength} символов."
            : null;
    }

    public static string? TopUpAmount(decimal amount)
    {
        if (amount <= 0)
            return "Сумма пополнения должна быть больше 0.";

        if (amount > 100_000)
            return "Слишком большая сумма пополнения.";

        return null;
    }

    public static string? Booking(DateTime startsAt, DateTime endsAt)
    {
        if (endsAt <= startsAt)
            return "Время окончания должно быть позже начала.";

        if (endsAt <= DateTime.Now)
            return "Нельзя создать бронь в прошлом.";

        if (endsAt - startsAt > TimeSpan.FromHours(24))
            return "Бронь не может быть дольше 24 часов.";

        return null;
    }

    public static string? CashAmount(decimal amount)
    {
        if (amount < 0)
            return "Сумма не может быть отрицательной.";

        if (amount > 1_000_000)
            return "Слишком большая сумма.";

        return null;
    }

    public static string? ClientRegistration(
        string firstName,
        string lastName,
        string? phoneNumber,
        string? email,
        DateOnly? birthDate,
        string? login,
        string? password)
    {
        var error = RequiredName(firstName, "имя");
        if (error is not null) return error;

        error = RequiredName(lastName, "фамилию");
        if (error is not null) return error;

        error = OptionalPhone(phoneNumber);
        if (error is not null) return error;

        error = OptionalEmail(email);
        if (error is not null) return error;

        if (birthDate is { } date && date > DateOnly.FromDateTime(DateTime.Now))
            return "Дата рождения не может быть в будущем.";

        error = Login(login, required: true);
        if (error is not null) return error;

        error = Password(password, required: true);
        return error;
    }

    public static string? ClientUpdate(
        string firstName,
        string lastName,
        string? phoneNumber,
        string? email,
        DateOnly? birthDate,
        string login,
        string? password)
    {
        var error = RequiredName(firstName, "имя");
        if (error is not null) return error;

        error = RequiredName(lastName, "фамилию");
        if (error is not null) return error;

        error = OptionalPhone(phoneNumber);
        if (error is not null) return error;

        error = OptionalEmail(email);
        if (error is not null) return error;

        if (birthDate is { } date && date > DateOnly.FromDateTime(DateTime.Now))
            return "Дата рождения не может быть в будущем.";

        error = Login(login, required: true);
        if (error is not null) return error;

        if (!string.IsNullOrWhiteSpace(password))
        {
            error = Password(password, required: true);
            if (error is not null) return error;
        }

        return null;
    }

    public static string? AuthCredentials(string? login, string? password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return "Введите логин и пароль.";

        return null;
    }

    public static string? ShiftCashAmount(decimal amount, bool allowZero = true)
    {
        if (amount < 0)
            return "Сумма не может быть отрицательной.";

        if (!allowZero && amount <= 0)
            return "Сумма должна быть больше 0.";

        if (amount > 1_000_000)
            return "Слишком большая сумма.";

        return null;
    }
}
