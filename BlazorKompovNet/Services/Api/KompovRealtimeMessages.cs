namespace BlazorKompovNet.Services.Api;

public sealed class KompovRealtimeChange
{
    public int? ClubId { get; set; }

    public int? ClientId { get; set; }

    public string? Scope { get; set; }
}
