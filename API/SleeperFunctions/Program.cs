using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SleeperFunctions.Application;

var builder = FunctionsApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(",") ?? [];

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient("SleeperClient", client =>
    client.BaseAddress = new Uri("https://api.sleeper.app/v1/"));

builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IRosterService, RosterService>();
builder.Services.AddScoped<IMatchupService, MatchupService>();
builder.Services.AddScoped<ILeagueService, LeagueService>();
builder.Services.AddScoped<IDraftService, DraftService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

});


builder.Build().Run();

