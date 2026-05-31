using BlazorKompovNet.Models;

namespace BlazorKompovNet.Services;

public static class BookingSessionPolicy
{
    public static readonly TimeSpan ActivationWindow = TimeSpan.FromHours(1);

    public static readonly TimeSpan ReleaseBuffer = TimeSpan.FromMinutes(30);

    public static bool IsRelevantStatus(string status) =>
        status is BookingStatus.Created or BookingStatus.Active;

    public static bool IsDisplayable(Booking booking, DateTime utcNow)
    {
        if (booking.EndsAt <= utcNow)
            return false;

        var clubToday = ClubDateTime.Today;
        var bookingStartLocal = ClubDateTime.ClubDate(booking.StartsAt);
        return bookingStartLocal == clubToday || bookingStartLocal == clubToday.AddDays(1);
    }

    public static bool IsBeforeBookingStart(Booking booking, DateTime utcNow) =>
        utcNow < booking.StartsAt;

    public static bool IsWithinActivationWindow(Booking booking, DateTime utcNow) =>
        utcNow >= booking.StartsAt - ActivationWindow && utcNow < booking.StartsAt;

    public static DateTime MaxSessionEndBeforeBooking(Booking booking) =>
        booking.StartsAt - ReleaseBuffer;

    public static string FormatBookingStart(Booking booking) =>
        ClubDateTime.Format(booking.StartsAt);

    public static string FormatBookingStartShort(Booking booking)
    {
        var localStart = ClubDateTime.ToClub(booking.StartsAt);
        return localStart.Date == ClubDateTime.Today
            ? $"сегодня {localStart:HH:mm}"
            : ClubDateTime.Format(booking.StartsAt);
    }

    public static BookingSessionValidation ValidateSessionStart(
        Booking? booking,
        int? clientId,
        DateTime utcNow,
        DateTime plannedEndAt)
    {
        if (booking is null || !IsBeforeBookingStart(booking, utcNow))
            return BookingSessionValidation.Allow(plannedEndAt);

        if (IsWithinActivationWindow(booking, utcNow))
        {
            if (clientId != booking.ClientId)
            {
                return BookingSessionValidation.Deny(
                    "За час до брони сеанс может запустить только клиент, оформивший бронь.");
            }

            return BookingSessionValidation.Allow(plannedEndAt);
        }

        var maxEnd = MaxSessionEndBeforeBooking(booking);
        if (utcNow >= maxEnd)
        {
            return BookingSessionValidation.Deny(
                $"Компьютер забронирован на {FormatBookingStart(booking)}. " +
                "До начала брони недостаточно времени для запуска сеанса.");
        }

        if (plannedEndAt > maxEnd)
        {
            return BookingSessionValidation.AllowWithCap(
                maxEnd,
                $"Компьютер забронирован на {FormatBookingStartShort(booking)}. " +
                $"Сеанс будет ограничен до {ClubDateTime.FormatTime(maxEnd)}, " +
                "чтобы компьютер освободился за 30 минут до брони.");
        }

        return BookingSessionValidation.Allow(
            plannedEndAt,
            $"Компьютер забронирован на {FormatBookingStartShort(booking)}. " +
            "Учитывайте, что сеанс должен завершиться за 30 минут до начала брони.");
    }

    public static BookingSessionValidation ValidateSessionExtension(
        Booking? booking,
        int? clientId,
        DateTime utcNow,
        DateTime proposedEndAt)
    {
        if (booking is null || !IsBeforeBookingStart(booking, utcNow))
            return BookingSessionValidation.Allow(proposedEndAt);

        if (IsWithinActivationWindow(booking, utcNow) && clientId == booking.ClientId)
            return BookingSessionValidation.Allow(proposedEndAt);

        var maxEnd = MaxSessionEndBeforeBooking(booking);
        if (utcNow >= maxEnd)
        {
            return BookingSessionValidation.Deny(
                $"Компьютер забронирован на {FormatBookingStart(booking)}. " +
                "Продление невозможно — до брони осталось менее 30 минут.");
        }

        if (proposedEndAt > maxEnd)
        {
            return BookingSessionValidation.AllowWithCap(
                maxEnd,
                $"Сеанс будет ограничен до {ClubDateTime.FormatTime(maxEnd)} из-за брони " +
                $"на {FormatBookingStartShort(booking)}.");
        }

        return BookingSessionValidation.Allow(proposedEndAt);
    }
}

public readonly struct BookingSessionValidation
{
    public bool IsAllowed { get; init; }

    public string? ErrorMessage { get; init; }

    public DateTime PlannedEndAt { get; init; }

    public string? NoticeMessage { get; init; }

    public static BookingSessionValidation Allow(DateTime plannedEndAt, string? notice = null) =>
        new() { IsAllowed = true, PlannedEndAt = plannedEndAt, NoticeMessage = notice };

    public static BookingSessionValidation AllowWithCap(DateTime plannedEndAt, string notice) =>
        new() { IsAllowed = true, PlannedEndAt = plannedEndAt, NoticeMessage = notice };

    public static BookingSessionValidation Deny(string message) =>
        new() { IsAllowed = false, ErrorMessage = message };
}
