using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Net;

namespace Shared.Services;

public sealed class LeagueState(ISleeperAPI sleeperApi, ILogger<LeagueState> logger)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly ILogger<LeagueState> _logger = logger;
    private Task? _loadTask;
    private bool _dataLoaded = false;
    
    private List<LeagueModel> _allLeagues = new List<LeagueModel>();

    /// <summary>
    /// List of all league details for this league. Verify using EnsureLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<LeagueModel> AllLeagues => _allLeagues;

    /// <summary>
    /// The current league_id for this league.
    /// </summary>
    public string CurrentLeagueId { get; private set; } = "-1";

    /// <summary>
    /// The current league's season.
    /// Set via the GetCurrentLeagueIdAsync() method.
    /// </summary>
    public string CurrentLeagueSeason { get; private set; } = "-1";

    /// <summary>
    /// The current league's current week.
    /// Set via the GetCurrentLeagueIdAsync() method.
    /// </summary>
    public string CurrentLeagueWeek { get; private set; } = "-1";

    /// <summary>
    /// Ensures AllLeagues data is loaded
    /// </summary>
    private bool IsLoaded => _dataLoaded;


    /// <summary>
    /// Sets all league data starting from the currentleagueid and looping backwards
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    private async Task SetAllLeaguesDataAsync(bool forceRefresh = false)
    {
        try
        {
            if (!IsLoaded || forceRefresh)
            {
                _allLeagues.Clear();
                var league_id = await GetCurrentLeagueIdAsync();
                CurrentLeagueId = league_id ?? "-1";
                while (!string.IsNullOrWhiteSpace(league_id))
                {
                    var league = await _sleeperApi.GetLeagueAsync(league_id ?? string.Empty);
                    if (league is not null)
                    {
                        _allLeagues.Add(league);
                    }
                    league_id = await GetPreviousLeagueIdAsync(league_id ?? string.Empty);
                }
            }
            _dataLoaded = true;
            
        }
        catch (Exception ex)
        {
            _loadTask = null;
            _dataLoaded = false;
            _logger.LogError(ex, "ERROR: {Message}", ex.Message);
            throw;
        }
    }


    /// <summary>
    /// Returns the currentleagueid for the league
    /// </summary>
    /// <returns></returns>
    public async Task<string?> GetCurrentLeagueIdAsync()
    {
        var nflState = await _sleeperApi.GetNFLState();

        if (nflState is not null && nflState?.LeagueSeason is not null)
        {
            CurrentLeagueSeason = nflState.LeagueSeason;
            CurrentLeagueWeek = nflState.Leg.ToString();
            var leagues = await _sleeperApi.GetLeagueBySeasonAsync(nflState.LeagueSeason);
            if (leagues is not null)
            {
                var league_id = leagues.FirstOrDefault(l => l?.Name?.Trim() == "Crush Cities")?.LeagueId ?? null;
                return league_id;
            }
        }
        return null;
    }


    /// <summary>
    /// Gets the previous leagueId from the provided leagueid
    /// </summary>
    /// <param name="league_id"></param>
    /// <returns></returns>
    public async Task<string?> GetPreviousLeagueIdAsync(string league_id)
    {
        if (string.IsNullOrWhiteSpace(league_id)) return null;
        try
        {
            var league = await _sleeperApi.GetLeagueAsync(league_id);
            if (league is null) return null;
            var prev = league.PreviousLeagueId;
            return string.IsNullOrWhiteSpace(prev) || prev == "0" ? null : prev;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Ensures that the AllLeagues data is loaded
    /// </summary>
    /// <returns></returns>
    public Task EnsureLoadedAsync()
    {
        if (IsLoaded) return Task.CompletedTask;
        _loadTask ??= SetAllLeaguesDataAsync(forceRefresh: true);
        return _loadTask;
    }
}
