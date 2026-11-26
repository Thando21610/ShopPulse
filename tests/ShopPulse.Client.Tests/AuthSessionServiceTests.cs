using System.Net;
using System.Text;
using ShopPulse.Client.Auth;
using Xunit;

namespace ShopPulse.Client.Tests;

public sealed class AuthSessionServiceTests
{
    [Fact]
    public async Task SignIn_UsesUserReturnedByLogin_WithoutSecondMeRequest()
    {
        var calls = 0;
        using var http = CreateClient((request, _) =>
        {
            calls++;
            Assert.EndsWith("/api/auth/login", request.RequestUri!.AbsolutePath);
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, LoginJson));
        });
        var tokens = new TokenStore();
        var session = new AuthSessionService(http, tokens);

        var result = await session.SignInAsync("owner@example.test", "test-password");

        Assert.True(result.Success, result.Message);
        Assert.Equal("access-token", tokens.AccessToken);
        Assert.Equal("Test Owner", session.User?.DisplayName);
        Assert.Equal("Owner", session.User?.Role);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Restore_WhenRefreshIsTemporarilyUnavailable_DoesNotClearToken()
    {
        using var http = CreateClient((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.ServiceUnavailable, "{}")));
        var tokens = new TokenStore();
        tokens.Set("previous-access-token");
        var session = new AuthSessionService(http, tokens);

        var restored = await session.RestoreAsync();

        Assert.False(restored);
        Assert.True(session.RestoreUnavailable);
        Assert.Equal("previous-access-token", tokens.AccessToken);
    }

    [Fact]
    public async Task Restore_WhenRefreshCookieIsRejected_ClearsLocalToken()
    {
        using var http = CreateClient((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.Unauthorized, "{}")));
        var tokens = new TokenStore();
        tokens.Set("expired-access-token");
        var session = new AuthSessionService(http, tokens);

        var restored = await session.RestoreAsync();

        Assert.False(restored);
        Assert.False(session.RestoreUnavailable);
        Assert.Null(tokens.AccessToken);
    }

    [Fact]
    public async Task ConcurrentRestoreCalls_UseOneRefreshAndOneProfileRequest()
    {
        var refreshCalls = 0;
        var profileCalls = 0;
        using var http = CreateClient(async (request, ct) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/api/auth/refresh", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref refreshCalls);
                await Task.Delay(40, ct);
                return JsonResponse(HttpStatusCode.OK, """{"accessToken":"restored-token"}""");
            }

            if (request.RequestUri.AbsolutePath.EndsWith("/api/auth/me", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref profileCalls);
                return JsonResponse(HttpStatusCode.OK, UserJson);
            }

            return JsonResponse(HttpStatusCode.NotFound, "{}");
        });
        var session = new AuthSessionService(http, new TokenStore());

        var results = await Task.WhenAll(session.RestoreAsync(), session.RestoreAsync());

        Assert.All(results, Assert.True);
        Assert.Equal(1, refreshCalls);
        Assert.Equal(1, profileCalls);
        Assert.True(session.IsAuthenticated);
        Assert.Equal("Test Owner", session.User?.DisplayName);
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        => new(new DelegateHandler(send)) { BaseAddress = new Uri("https://api.example.test/") };

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private const string UserJson = """
        {
          "id":"11111111-1111-1111-1111-111111111111",
          "displayName":"Test Owner",
          "email":"owner@example.test",
          "role":"Owner",
          "businessId":"22222222-2222-2222-2222-222222222222",
          "businessName":"Test Store",
          "canAdjustStock":true,
          "canRefundSales":true,
          "canManageProducts":true,
          "canReceiveStock":true
        }
        """;

    private const string LoginJson = """
        {
          "accessToken":"access-token",
          "expiresAtUtc":"2026-11-24T12:00:00Z",
          "user":{
            "id":"11111111-1111-1111-1111-111111111111",
            "displayName":"Test Owner",
            "email":"owner@example.test",
            "role":"Owner",
            "businessId":"22222222-2222-2222-2222-222222222222",
            "businessName":"Test Store",
            "canAdjustStock":true,
            "canRefundSales":true,
            "canManageProducts":true,
            "canReceiveStock":true
          }
        }
        """;

    private sealed class DelegateHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => send(request, cancellationToken);
    }
}
