using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CrushCitiesFFL;
using Radzen;
using Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => 
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }
);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

builder.Services.AddHttpClient<ISleeperAPI, SleeperFunctionsAPI>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["SleeperFunctionsUrl"]!);
});

builder.Services.AddScoped<RosterData>();
builder.Services.AddScoped<PlayerData>();
builder.Services.AddScoped<LeagueData>();
builder.Services.AddScoped<UserData>();
builder.Services.AddScoped<DraftData>();
builder.Services.AddScoped<MatchupData>();
builder.Services.AddScoped<TransactionData>();
builder.Services.AddScoped<PlayoffData>();
builder.Services.AddScoped<IStatsData, StatsData>();
builder.Services.AddScoped<IRosterStats, RosterStats>();
builder.Services.AddScoped<IMatchupStats, MatchupStats>();
builder.Services.AddScoped<IPlayoffStats, PlayoffStats>();
builder.Services.AddScoped<ITransactionStats, TransactionStats>();
builder.Services.AddScoped<INormalizer, Normalizer>();
builder.Services.AddBlazorBootstrap();
builder.Services.AddRadzenComponents();

var host = builder.Build();
//Set initial state for models 
var playerData = host.Services.GetRequiredService<PlayerData>();
var userData = host.Services.GetRequiredService<UserData>();
var rosterData = host.Services.GetRequiredService<RosterData>();
var leagueData = host.Services.GetRequiredService<LeagueData>();
await leagueData.EnsureLoadedAsync();
await userData.EnsureLoadedAsync();
await rosterData.EnsureLoadedAsync();
await playerData.EnsureLoadedAsync();


await host.RunAsync();
