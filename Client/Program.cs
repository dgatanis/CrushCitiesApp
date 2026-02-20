using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Client;
using Client.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<AuthHeaderHandler>();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddHttpClient("ServerAPI", client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7016";
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerAPI"));

await builder.Build().RunAsync();
