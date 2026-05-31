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

        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    public async Task PostAsync(string path, object body, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(
            () => httpClient.PostAsJsonAsync(BuildUri(path), body, JsonOptions, cancellationToken),
            cancellationToken);

        await EnsureSuccessOrThrowAsync(response, cancellationToken);
    }

    public async Task PutAsync(string path, object body, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(
            () => httpClient.PutAsJsonAsync(BuildUri(path), body, JsonOptions, cancellationToken),
            cancellationToken);

        await EnsureSuccessOrThrowAsync(response, cancellationToken);
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
            "Если Blazor и API на одном ПК — используйте http://10.80.104.157:5232, не IP сети и не localhost.",
            inner);

    private static string Combine(string prefix, string relativePath)
    {
        var left = prefix.Trim('/');
        var right = relativePath.Trim('/');
        return string.IsNullOrEmpty(right) ? left : $"{left}/{right}";
    }

    private static async Task EnsureSuccessOrThrowAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var details = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = TryExtractApiErrorMessage(details)
            ?? $"Запрос завершился с кодом {(int)response.StatusCode} ({response.StatusCode}).";

        if (details.Contains("FK_GameSessions_Clients_ClientId", StringComparison.Ordinal))
        {
            message =
                "База данных API не обновлена: нужна миграция для гостевых сессий без аккаунта. " +
                "Перезапустите KompovNetApi из актуальной версии репозитория.";
        }

        throw new InvalidOperationException(message);
    }

    private static string? TryExtractApiErrorMessage(string details)
    {
        if (string.IsNullOrWhiteSpace(details))
        {
            return null;
        }

        const string marker = "Exception:";
        var index = details.LastIndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            return details.Length > 300 ? details[..300] : details;
        }

        var excerpt = details[(index + marker.Length)..].Trim();
        var lineBreak = excerpt.IndexOf('\n');
        if (lineBreak > 0)
        {
            excerpt = excerpt[..lineBreak].Trim();
        }

        return string.IsNullOrWhiteSpace(excerpt) ? null : excerpt;
    }
}
