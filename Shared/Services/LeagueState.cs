using Shared.Models;
using System.Net;

namespace Shared.Services;

public sealed class LeagueState(ISleeperAPI sleeperApi)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private Task? _loadTask;

    /// <summary>
    /// List of all league details for this league.
    /// Set via SetAllLeaguesDataAsync()
    /// </summary>
    public List<LeagueModel> AllLeagues { get; private set; } = new();

    /// <summary>
    /// Ensures AllLeagues is loaded
    /// </summary>
    public bool IsLoaded => AllLeagues is not null && AllLeagues.Count > 0;

    /// <summary>
    /// The current league_id for this league.
    /// Set via SetAllLeaguesDataAsync()
    /// </summary>
    public string CurrentLeagueId { get; private set; } = "-1";


    /// <summary>
    /// Sets all league dataq based starting from the currentleagueid and looping backwards
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task SetAllLeaguesDataAsync(bool forceRefresh = false)
    {
        if (!IsLoaded || forceRefresh)
        {
            var league_id = await GetCurrentLeagueIdAsync();
            CurrentLeagueId = league_id ?? "-1";
            while (!string.IsNullOrWhiteSpace(league_id))
            {

                    var league = await _sleeperApi.GetLeagueAsync(league_id ?? string.Empty);
                    if (league is not null)
                    {
                        AllLeagues.Add(league);
                    }
                
                league_id = await GetPreviousLeagueIdAsync(league_id ?? string.Empty);
            }
        }
            
    }


    /// <summary>
    /// Returns the currentleagueid for the league
    /// </summary>
    /// <returns></returns>
    public async Task<string?> GetCurrentLeagueIdAsync()
    {
        var nflState = await _sleeperApi.GetNFLState();
        if (nflState?.LeagueSeason is not null)
        {
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
    /// Ensures that the All Leagues data is loaded
    /// </summary>
    /// <returns></returns>
    public Task EnsureLoadedAsync()
    {
        if (IsLoaded) return Task.CompletedTask;
        _loadTask ??= SetAllLeaguesDataAsync(forceRefresh: true);
        return _loadTask;
    }
}
