using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace BlazorKompovNet.Services.Api;

public sealed class KompovApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new UtcDateTimeJsonConverter(),
            new NullableUtcDateTimeJsonConverter()
        }
    };

    private readonly HttpClient httpClient;
    private readonly ApiOptions options;

    public KompovApiClient(HttpClient httpClient, IOptions<ApiOptions> options)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
    }

    public string Admin(string relativePath) => Combine(options.AdminPanelPath, relativePath);

    public string Crm(string relativePath) => Combine(options.BlazorCrmPath, relativePath);

    public string Mobile(string relativePath) => Combine(options.MobileAppPath, relativePath);

    public async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(
            () => httpClient.GetAsync(BuildUri(path), cancellationToken),
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetListAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(
            () => httpClient.GetAsync(BuildUri(path), cancellationToken),
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<T>>(JsonOptions, cancellationToken) ?? [];
    }

    public async Task<T?> PostAsync<T>(string path, object body, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(
            () => httpClient.PostAsJsonAsync(BuildUri(path), body, JsonOptions, cancellationToken),
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    public async Task PostAsync(string path, object body, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(
            () => httpClient.PostAsJsonAsync(BuildUri(path), body, JsonOptions, cancellationToken),
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task PutAsync(string path, object body, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(
            () => httpClient.PutAsJsonAsync(BuildUri(path), body, JsonOptions, cancellationToken),
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(
            () => httpClient.DeleteAsync(BuildUri(path), cancellationToken),
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private Uri BuildUri(string path)
    {
        var baseUrl = options.BaseUrl.TrimEnd('/');
        var relativePath = path.TrimStart('/');
        return new Uri($"{baseUrl}/{relativePath}", UriKind.Absolute);
    }

    private async Task<HttpResponseMessage> SendAsync(
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                return await send();
            }
            catch (Exception ex) when (IsTransientConnectionError(ex) && attempt == 0)
            {
                await Task.Delay(300, cancellationToken);
            }
            catch (Exception ex) when (IsTransientConnectionError(ex))
            {
                throw CreateConnectionException(ex);
            }
        }

        throw new InvalidOperationException("Не удалось выполнить запрос к API.");
    }

    private static bool IsTransientConnectionError(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or IOException
        || ex.InnerException is SocketException or IOException;

    private InvalidOperationException CreateConnectionException(Exception inner) =>
        new(
            $"Не удалось подключиться к API ({options.BaseUrl}). " +
            "Убедитесь, что KompovNetApi запущен. " +
            "Если Blazor и API на одном ПК — используйте http://127.0.0.1:5232, не IP сети и не localhost.",
            inner);

    private static string Combine(string prefix, string relativePath)
    {
        var left = prefix.Trim('/');
        var right = relativePath.Trim('/');
        return string.IsNullOrEmpty(right) ? left : $"{left}/{right}";
    }
}
