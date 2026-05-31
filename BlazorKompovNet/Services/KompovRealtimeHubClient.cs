using BlazorKompovNet.Services.Api;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace BlazorKompovNet.Services;

public sealed class KompovRealtimeHubClient : IAsyncDisposable
{
    private readonly ApiOptions _options;
    private readonly ILogger<KompovRealtimeHubClient> _logger;
    private readonly List<HubConnection> _connections = [];
    private int? _clubId;
    private int? _clientId;

    public KompovRealtimeHubClient(IOptions<ApiOptions> options, ILogger<KompovRealtimeHubClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public event Func<KompovRealtimeChange, Task>? DataChanged;

    public async Task EnsureConnectedAsync(int? clubId = null, int? clientId = null, CancellationToken cancellationToken = default)
    {
        _clubId = clubId ?? _clubId;
        _clientId = clientId ?? _clientId;

        if (_connections.Count > 0 && _connections.All(c => c.State == HubConnectionState.Connected))
        {
            await JoinGroupsAsync(cancellationToken);
            return;
        }

        await DisposeConnectionsAsync();

        foreach (var hubPath in GetHubPaths())
        {
            try
            {
                var connection = new HubConnectionBuilder()
                    .WithUrl($"{_options.BaseUrl.TrimEnd('/')}{hubPath}")
                    .WithAutomaticReconnect()
                    .Build();

                RegisterHandlers(connection);
                connection.Reconnected += _ => JoinGroupsOnConnectionAsync(connection);
                await connection.StartAsync(cancellationToken);
                _connections.Add(connection);
                await JoinGroupsOnConnectionAsync(connection);
                _logger.LogInformation("Blazor connected to {HubUrl}", hubPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not connect Blazor to {HubUrl}", hubPath);
            }
        }
    }

    private IEnumerable<string> GetHubPaths() =>
    [
        "/hubs/kompov",
        "/hubs/admin-panel",
        "/hubs/mobile-app"
    ];

    private void RegisterHandlers(HubConnection connection)
    {
        string[] events =
        [
            "InitialSnapshot",
            "ClientBalanceChanged",
            "BalanceUpdated",
            "TransactionCreated",
            "TransactionsUpdated",
            "ComputersUpdated",
            "BookingUpdated",
            "SessionUpdated",
            "RefreshData",
            "DataChanged"
        ];

        foreach (var eventName in events)
        {
            connection.On(eventName, () => NotifyAsync(eventName));
        }
    }

    private async Task JoinGroupsAsync(CancellationToken cancellationToken)
    {
        foreach (var connection in _connections.Where(c => c.State == HubConnectionState.Connected))
            await JoinGroupsOnConnectionAsync(connection, cancellationToken);
    }

    private Task JoinGroupsOnConnectionAsync(HubConnection connection) =>
        JoinGroupsOnConnectionAsync(connection, CancellationToken.None);

    private async Task JoinGroupsOnConnectionAsync(HubConnection connection, CancellationToken cancellationToken)
    {
        if (_clientId is not > 0 || _clubId is not > 0)
            return;

        var clientId = _clientId.Value;
        var clubId = _clubId.Value;

        string[] methods = ["JoinAdmin", "JoinClub", "JoinClient", "Subscribe", "SubscribeToClub", "SubscribeToClient"];
        foreach (var method in methods)
        {
            try
            {
                await connection.InvokeAsync(method, clientId, clubId, cancellationToken);
                return;
            }
            catch
            {
                // try next hub method name
            }
        }
    }

    private Task NotifyAsync(string scope)
    {
        var handler = DataChanged;
        if (handler is null)
            return Task.CompletedTask;

        return handler.Invoke(new KompovRealtimeChange
        {
            ClubId = _clubId,
            ClientId = _clientId,
            Scope = scope
        });
    }

    private async Task DisposeConnectionsAsync()
    {
        foreach (var connection in _connections)
            await connection.DisposeAsync();

        _connections.Clear();
    }

    public async ValueTask DisposeAsync() => await DisposeConnectionsAsync();
}
