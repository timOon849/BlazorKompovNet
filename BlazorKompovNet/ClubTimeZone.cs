namespace BlazorKompovNet;

/// <summary>Часовой пояс клуба: GMT+3 (как в API).</summary>
public static class ClubTimeZone
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(3);

    public static readonly TimeZoneInfo Zone =
        TimeZoneInfo.CreateCustomTimeZone("GMT+3", Offset, "GMT+3", "GMT+3");

    public static DateTime Now => FromUtc(DateTime.UtcNow);

    public static DateTime FromUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            Zone);

    public static DateTime StartOfTodayUtc() => ToUtc(Now.Date);

    public static DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(local, DateTimeKind.Unspecified),
            Zone);
}
