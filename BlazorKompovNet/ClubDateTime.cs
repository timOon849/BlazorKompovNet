namespace BlazorKompovNet;

public static class ClubDateTime
{
    public static DateTime UtcNow => DateTime.UtcNow;

    public static DateTime Now => ClubTimeZone.Now;

    public static DateTime Today => Now.Date;

    public static DateTime StartOfTodayUtc() => ClubTimeZone.StartOfTodayUtc();

    public static DateTime ToClub(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => ClubTimeZone.FromUtc(value),
            DateTimeKind.Local => ClubTimeZone.FromUtc(value.ToUniversalTime()),
            _ => value
        };

    public static DateTime ToUtc(DateTime clubLocal) => ClubTimeZone.ToUtc(clubLocal);

    public static DateTime ClubDate(DateTime value) => ToClub(value).Date;

    public static string Format(DateTime value, string format = "dd.MM.yyyy HH:mm") =>
        ToClub(value).ToString(format);

    public static string FormatTime(DateTime value) => ToClub(value).ToString("HH:mm");
}
