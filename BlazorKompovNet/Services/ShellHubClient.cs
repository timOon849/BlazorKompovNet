using BlazorKompovNet.Services.Api;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace BlazorKompovNet.Services;

public sealed class ShellHubClient : IAsyncDisposable
{
    private readonly ApiOptions _options;
    private readonly ILogger<ShellHubClient> _logger;
    private HubConnection? _connection;

    public ShellHubClient(IOptions<ApiOptions> options, ILogger<ShellHubClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public event Func<Task>? SessionChanged;

    public async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        if (_connection?.State == HubConnectionState.Connected)
            return;

        if (_connection is not null)
            await _connection.DisposeAsync();

        _connection = new HubConnectionBuilder()
            .WithUrl($"{_options.BaseUrl.TrimEnd('/')}/hubs/shell")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<ShellSessionStartedMessage>("SessionStarted", _ => NotifySessionChangedAsync());
        _connection.On<ShellSessionEndedMessage>("SessionEnded", _ => NotifySessionChangedAsync());

        _connection.Reconnected += _ => RegisterMonitorAsync();
        await _connection.StartAsync(cancellationToken);
        await RegisterMonitorAsync();
        _logger.LogInformation("Blazor connected to shell SignalR hub.");
    }

    private async Task RegisterMonitorAsync()
    {
        if (_connection?.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("RegisterMonitor");
    }

    private Task NotifySessionChangedAsync()
    {
        var handler = SessionChanged;
        return handler is null ? Task.CompletedTask : handler();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
