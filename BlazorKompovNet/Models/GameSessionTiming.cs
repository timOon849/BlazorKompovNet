namespace BlazorKompovNet.Models;

public static class GameSessionTiming
{
    public static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(5);

    public static DateTime GetAutoCompleteAt(GameSession session) =>
        session.PlannedEndAt.Add(GracePeriod);

    public static bool IsInGracePeriod(GameSession session) =>
        session.Status == GameSessionStatus.Active
        && DateTime.UtcNow >= session.PlannedEndAt
        && DateTime.UtcNow < GetAutoCompleteAt(session);

    public static bool ShouldAutoComplete(GameSession session) =>
        session.Status == GameSessionStatus.Active
        && DateTime.UtcNow >= GetAutoCompleteAt(session);

    public static bool IsPastPlannedEnd(GameSession session) =>
        session.Status == GameSessionStatus.Active
        && DateTime.UtcNow >= session.PlannedEndAt;

    public static TimeSpan? GetRemainingTime(GameSession session)
    {
        if (session.Status != GameSessionStatus.Active)
        {
            return null;
        }

        var remaining = session.PlannedEndAt - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public static TimeSpan? GetGraceTimeLeft(GameSession session)
    {
        if (!IsInGracePeriod(session))
        {
            return null;
        }

        return GetAutoCompleteAt(session) - DateTime.UtcNow;
    }
}
