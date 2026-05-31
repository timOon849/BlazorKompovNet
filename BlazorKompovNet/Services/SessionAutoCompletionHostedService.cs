namespace BlazorKompovNet.Services;

public sealed class SessionAutoCompletionHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<SessionAutoCompletionHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var clubManagement = scope.ServiceProvider.GetRequiredService<IClubManagementService>();
                var completedIds = await clubManagement.CompleteExpiredSessionsAsync();

                if (completedIds.Count > 0)
                {
                    logger.LogInformation(
                        "Автоматически завершено сессий: {Count} ({SessionIds})",
                        completedIds.Count,
                        string.Join(", ", completedIds));
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Не удалось проверить просроченные игровые сессии.");
            }
        }
    }
}
