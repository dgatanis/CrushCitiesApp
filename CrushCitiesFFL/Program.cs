using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CrushCitiesFFL;
using Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://api.sleeper.app/v1/") });
builder.Services.AddScoped<ISleeperAPI, SleeperAPI>();
builder.Services.AddScoped<RosterState>();
builder.Services.AddScoped<PlayerState>();

await builder.Build().RunAsync();
