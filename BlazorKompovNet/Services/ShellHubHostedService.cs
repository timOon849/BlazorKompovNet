namespace BlazorKompovNet.Services;

public sealed class ShellHubHostedService(ShellHubClient hubClient, ILogger<ShellHubHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await hubClient.EnsureConnectedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not connect Blazor to shell SignalR hub on startup.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
