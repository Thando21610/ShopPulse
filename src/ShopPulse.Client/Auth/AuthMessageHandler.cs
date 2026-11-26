using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace ShopPulse.Client.Auth;

public sealed class AuthMessageHandler : DelegatingHandler
{
    private readonly TokenStore _tokens;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private long _refreshAttemptVersion;
    private int _lastRefreshStatusCode = (int)HttpStatusCode.ServiceUnavailable;
    private readonly Uri _apiBase;

    public AuthMessageHandler(TokenStore tokens, IConfiguration configuration)
    {
        _tokens = tokens;
        var configured = configuration["ApiBaseUrl"];
        _apiBase = new Uri(string.IsNullOrWhiteSpace(configured) ? "/" : configured.TrimEnd('/') + "/",
            string.IsNullOrWhiteSpace(configured) ? UriKind.RelativeOrAbsolute : UriKind.Absolute);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        var path = request.RequestUri?.AbsolutePath ?? "";
        var isAuthEndpoint = path.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/api/auth/refresh", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/api/auth/logout", StringComparison.OrdinalIgnoreCase);

        var tokenUsed = _tokens.AccessToken;
        var refreshVersionAtSend = Volatile.Read(ref _refreshAttemptVersion);
        if (!string.IsNullOrWhiteSpace(tokenUsed))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenUsed);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || isAuthEndpoint)
            return response;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var currentToken = _tokens.AccessToken;
            if (!string.IsNullOrWhiteSpace(currentToken) && currentToken != tokenUsed)
            {
                response.Dispose();
                return await RetryWithTokenAsync(request, currentToken, cancellationToken);
            }

            if (Volatile.Read(ref _refreshAttemptVersion) != refreshVersionAtSend)
            {
                if (_lastRefreshStatusCode == (int)HttpStatusCode.Unauthorized
                    || _lastRefreshStatusCode == (int)HttpStatusCode.Forbidden)
                    return response;

                response.Dispose();
                return CreateUnavailableResponse(request);
            }

            using var refresh = new HttpRequestMessage(HttpMethod.Post, new Uri(_apiBase, "api/auth/refresh"));
            refresh.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            HttpResponseMessage refreshResponse;
            try
            {
                refreshResponse = await base.SendAsync(refresh, cancellationToken);
            }
            catch (HttpRequestException)
            {
                Volatile.Write(ref _lastRefreshStatusCode, (int)HttpStatusCode.ServiceUnavailable);
                response.Dispose();
                return CreateUnavailableResponse(request);
            }
            finally
            {
                Interlocked.Increment(ref _refreshAttemptVersion);
            }

            if (refreshResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                Volatile.Write(ref _lastRefreshStatusCode, (int)refreshResponse.StatusCode);
                refreshResponse.Dispose();
                _tokens.Clear();
                return response;
            }

            if (!refreshResponse.IsSuccessStatusCode)
            {
                Volatile.Write(ref _lastRefreshStatusCode, (int)refreshResponse.StatusCode);
                response.Dispose();
                return refreshResponse;
            }

            string? nextToken;
            try
            {
                using var payload = await refreshResponse.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
                nextToken = payload?.RootElement.TryGetProperty("accessToken", out var value) == true
                    ? value.GetString() : null;
            }
            catch (JsonException)
            {
                Volatile.Write(ref _lastRefreshStatusCode, (int)HttpStatusCode.BadGateway);
                refreshResponse.Dispose();
                response.Dispose();
                return CreateUnavailableResponse(request, HttpStatusCode.BadGateway);
            }

            if (string.IsNullOrWhiteSpace(nextToken))
            {
                Volatile.Write(ref _lastRefreshStatusCode, (int)HttpStatusCode.BadGateway);
                refreshResponse.Dispose();
                response.Dispose();
                return CreateUnavailableResponse(request, HttpStatusCode.BadGateway);
            }

            Volatile.Write(ref _lastRefreshStatusCode, (int)HttpStatusCode.OK);
            refreshResponse.Dispose();
            _tokens.Set(nextToken);
            response.Dispose();
            return await RetryWithTokenAsync(request, nextToken, cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static HttpResponseMessage CreateUnavailableResponse(
        HttpRequestMessage request, HttpStatusCode statusCode = HttpStatusCode.ServiceUnavailable)
        => new(statusCode)
        {
            RequestMessage = request,
            ReasonPhrase = "Authentication refresh is temporarily unavailable."
        };

    private async Task<HttpResponseMessage> RetryWithTokenAsync(HttpRequestMessage original, string token, CancellationToken ct)
    {
        var retry = await CloneAsync(original, ct);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        retry.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return await base.SendAsync(retry, ct);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(ct);
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }
}
