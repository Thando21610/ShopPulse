using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShopPulse.Client;
using ShopPulse.Client.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

try
{
    using var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    using var stream = await http.GetStreamAsync("appsettings.json");
    builder.Configuration.AddJsonStream(stream);
}
catch { /* optional config */ }

var apiBase = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrWhiteSpace(apiBase) || apiBase.Contains("YOUR-API-URL", StringComparison.OrdinalIgnoreCase))
    apiBase = builder.HostEnvironment.BaseAddress;
apiBase = apiBase.TrimEnd('/') + "/";
builder.Configuration["ApiBaseUrl"] = apiBase;

builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<AuthMessageHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(apiBase) };
});
builder.Services.AddScoped<AuthSessionService>();

await builder.Build().RunAsync();
