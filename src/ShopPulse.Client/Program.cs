using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShopPulse.Client;

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

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(apiBase.TrimEnd('/') + "/")
});

await builder.Build().RunAsync();
