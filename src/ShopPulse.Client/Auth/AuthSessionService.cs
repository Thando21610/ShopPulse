using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShopPulse.Client.Auth;

public sealed record SignedInUser(Guid Id, string DisplayName, string Email, string Role, Guid BusinessId, string BusinessName, bool CanAdjustStock, bool CanRefundSales, bool CanManageProducts, bool CanReceiveStock);

public sealed class AuthSessionService
{
    private readonly HttpClient _http;
    private readonly TokenStore _tokens;
    private readonly SemaphoreSlim _restoreLock = new(1, 1);

    public AuthSessionService(HttpClient http, TokenStore tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    public SignedInUser? User { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(_tokens.AccessToken) && User is not null;
    public bool RestoreUnavailable { get; private set; }

    public async Task<bool> RestoreAsync(CancellationToken ct = default)
    {
        if (IsAuthenticated) return true;

        await _restoreLock.WaitAsync(ct);
        try
        {
            if (IsAuthenticated) return true;
            RestoreUnavailable = false;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            var tokenCt = timeout.Token;

            try
            {
                using var response = await _http.PostAsync("api/auth/refresh", null, tokenCt);
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    Clear();
                    return false;
                }

                if (!response.IsSuccessStatusCode)
                {
                    RestoreUnavailable = true;
                    return false;
                }

                using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: tokenCt);
                var token = payload?.RootElement.TryGetProperty("accessToken", out var value) == true ? value.GetString() : null;
                if (string.IsNullOrWhiteSpace(token))
                {
                    RestoreUnavailable = true;
                    return false;
                }

                _tokens.Set(token);
                using var meResponse = await _http.GetAsync("api/auth/me", tokenCt);
                if (meResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    Clear();
                    return false;
                }

                if (!meResponse.IsSuccessStatusCode)
                {
                    RestoreUnavailable = true;
                    return false;
                }

                User = await meResponse.Content.ReadFromJsonAsync<SignedInUser>(cancellationToken: tokenCt);
                if (User is null)
                {
                    Clear();
                    RestoreUnavailable = true;
                    return false;
                }

                return true;
            }
            catch (HttpRequestException)
            {
                RestoreUnavailable = true;
                return false;
            }
            catch (JsonException)
            {
                RestoreUnavailable = true;
                return false;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                RestoreUnavailable = true;
                return false;
            }
        }
        finally
        {
            _restoreLock.Release();
        }
    }

    public async Task<(bool Success, string? Message)> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password }, ct);
            if (!response.IsSuccessStatusCode)
            {
                var message = response.StatusCode == HttpStatusCode.Unauthorized
                    ? "The email or password is incorrect. Check your details and try again."
                    : "We couldn't sign you in right now. Please try again.";
                return (false, message);
            }

            using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            var root = payload?.RootElement;
            var token = root?.TryGetProperty("accessToken", out var value) == true ? value.GetString() : null;
            if (string.IsNullOrWhiteSpace(token) || root?.TryGetProperty("user", out var userValue) != true)
                return (false, "The server returned an unexpected sign-in response.");

            var user = userValue.Deserialize<SignedInUser>();
            if (user is null)
                return (false, "Your account response is incomplete. Please contact your administrator.");

            _tokens.Set(token);
            User = user;
            RestoreUnavailable = false;
            return (true, null);
        }
        catch (HttpRequestException)
        {
            Clear();
            return (false, "We couldn't reach ShopPulse. Check your connection and try again.");
        }
        catch (JsonException)
        {
            Clear();
            return (false, "The server returned an unexpected response. Please try again.");
        }
        catch (OperationCanceledException)
        {
            Clear();
            return (false, "Sign-in timed out. Check your connection and try again.");
        }
    }

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        try { using var response = await _http.PostAsync("api/auth/logout", null, ct); }
        catch (HttpRequestException) { }
        finally { Clear(); }
    }

    public void Clear()
    {
        _tokens.Clear();
        User = null;
        RestoreUnavailable = false;
    }
}
