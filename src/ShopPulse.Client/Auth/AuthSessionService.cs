using System.Net.Http.Json;
using System.Text.Json;

namespace ShopPulse.Client.Auth;

public sealed record SignedInUser(Guid Id, string DisplayName, string Email, string Role, Guid BusinessId, string BusinessName, bool CanAdjustStock, bool CanRefundSales, bool CanManageProducts, bool CanReceiveStock);

public sealed class AuthSessionService
{
    private readonly HttpClient _http;
    private readonly TokenStore _tokens;

    public AuthSessionService(HttpClient http, TokenStore tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    public SignedInUser? User { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(_tokens.AccessToken) && User is not null;

    public async Task<bool> RestoreAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.PostAsync("api/auth/refresh", null, ct);
            if (!response.IsSuccessStatusCode)
            {
                Clear();
                return false;
            }
            using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            var token = payload?.RootElement.TryGetProperty("accessToken", out var value) == true ? value.GetString() : null;
            if (string.IsNullOrWhiteSpace(token))
            {
                Clear();
                return false;
            }
            _tokens.Set(token);
            User = await _http.GetFromJsonAsync<SignedInUser>("api/auth/me", ct);
            if (User is null) Clear();
            return User is not null;
        }
        catch (HttpRequestException)
        {
            Clear();
            return false;
        }
        catch (JsonException)
        {
            Clear();
            return false;
        }
    }

    public async Task<(bool Success, string? Message)> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password }, ct);
            if (!response.IsSuccessStatusCode)
            {
                var message = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "The email or password is incorrect. Check your details and try again."
                    : "We couldn't sign you in right now. Please try again.";
                return (false, message);
            }

            using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            var token = payload?.RootElement.TryGetProperty("accessToken", out var value) == true ? value.GetString() : null;
            if (string.IsNullOrWhiteSpace(token))
                return (false, "The server returned an unexpected sign-in response.");
            _tokens.Set(token);
            User = await _http.GetFromJsonAsync<SignedInUser>("api/auth/me", ct);
            if (User is null)
            {
                Clear();
                return (false, "Your account response is incomplete. Please contact your administrator.");
            }
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
    }
}
