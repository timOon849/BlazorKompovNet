namespace BlazorKompovNet.Services;

public sealed class KompovRealtimeHostedService(
    KompovRealtimeHubClient hubClient,
    ILogger<KompovRealtimeHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await hubClient.EnsureConnectedAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not connect Blazor to Kompov realtime hub on startup.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
