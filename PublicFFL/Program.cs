using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PublicFFL;
using Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => 
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }
);
builder.Services.AddHttpClient<ISleeperAPI, SleeperAPI>(client =>
{
    client.BaseAddress = new Uri("https://api.sleeper.app/v1/");
});

builder.Services.AddScoped<RosterState>();
builder.Services.AddScoped<PlayerState>();
builder.Services.AddScoped<LeagueState>();
builder.Services.AddScoped<UserState>();
builder.Services.AddScoped<DraftState>();
builder.Services.AddScoped<MatchupState>();
builder.Services.AddScoped<TransactionState>();
builder.Services.AddScoped<PlayoffState>();
builder.Services.AddScoped<StatsData>();
builder.Services.AddScoped<INormalizer, Normalizer>();
builder.Services.AddBlazorBootstrap();

var host = builder.Build();
//Set initial state for models 
var playerState = host.Services.GetRequiredService<PlayerState>();
var userState = host.Services.GetRequiredService<UserState>();
var rosterState = host.Services.GetRequiredService<RosterState>();
var leagueState = host.Services.GetRequiredService<LeagueState>();
await leagueState.SetAllLeaguesDataAsync(forceRefresh: true);
await userState.SetCurrentUsersAsync(forceRefresh: true);
await rosterState.SetCurrentRostersAsync(forceRefresh: true);
await playerState.SetPlayersAsync(forceRefresh: true);


await host.RunAsync();