namespace ShopPulse.Client.Auth;

public sealed class TokenStore
{
    public string? AccessToken { get; private set; }
    public void Set(string token) => AccessToken = token;
    public void Clear() => AccessToken = null;
}
