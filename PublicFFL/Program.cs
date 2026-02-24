using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PublicFFL;
using Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient<ISleeperAPI, SleeperAPI>(client =>
{
    client.BaseAddress = new Uri("https://api.sleeper.app/v1/");
});
builder.Services.AddScoped<RosterState>();
builder.Services.AddScoped<PlayerState>();
builder.Services.AddScoped<LeagueState>();
builder.Services.AddScoped<UserState>();

var host = builder.Build();
var playerState = host.Services.GetRequiredService<PlayerState>();
await playerState.SetPlayers(forceRefresh: true);

await host.RunAsync();