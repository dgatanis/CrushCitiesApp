using Shared.Models;

namespace Shared.Services;

public sealed class LeagueState(ISleeperAPI sleeperApi)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    public LeagueModel? League { get; private set; }
    public bool IsLoaded => League is not null;

    public async Task SetLeague(string league_id, bool forceRefresh = false)
    {
        if (!IsLoaded || forceRefresh)
        {
            var league = await _sleeperApi.GetLeagueAsync(league_id ?? string.Empty);
            if (league is not null)
            {
                League = league;
            }
        }
    }

    public async Task<string?> GetCurrentLeagueId()
    {
        var nflState = await _sleeperApi.GetNFLState();
        if (nflState?.LeagueSeason is not null)
        {
            var leagues = await _sleeperApi.GetLeagueBySeason(nflState.LeagueSeason);
            if (leagues is not null)
            {
                var league_id = leagues.FirstOrDefault(l => l?.Name.Trim() == "Crush Cities")?.LeagueId ?? null;
                return league_id;
            }
        }
        return null;
    }

    private async Task EnsureLoadedAsync(bool forceRefresh = false)
    {
        if (IsLoaded && !forceRefresh) return;
        var leagueId = await GetCurrentLeagueId();
        if (string.IsNullOrWhiteSpace(leagueId))
        {
            throw new InvalidOperationException("Current league id is not available.");
        }
        await SetLeague(leagueId, true);
    }
}
