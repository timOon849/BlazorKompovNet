namespace BlazorKompovNet.Services.Api;

public sealed class ShellSessionStartedMessage
{
    public int SessionId { get; set; }

    public int ComputerId { get; set; }
}

public sealed class ShellSessionEndedMessage
{
    public int SessionId { get; set; }

    public int ComputerId { get; set; }
}
