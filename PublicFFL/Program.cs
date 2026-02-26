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
//Set initial state for models 
var playerState = host.Services.GetRequiredService<PlayerState>();
var userState = host.Services.GetRequiredService<UserState>();
var rosterState = host.Services.GetRequiredService<RosterState>();
var leagueId = await host.Services.GetRequiredService<LeagueState>().GetCurrentLeagueId();
if (string.IsNullOrWhiteSpace(leagueId))
{
    throw new InvalidOperationException("Current league id is not available.");
}
await playerState.SetPlayers(forceRefresh: true);
await rosterState.SetRosters(leagueId, forceRefresh: true);
await userState.SetUsers(leagueId, forceRefresh: true);

await host.RunAsync();