using Shared.Models;

namespace Shared.Services;

public sealed class RosterState(ISleeperAPI sleeperApi, LeagueState leagueState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;
    public List<RostersModel>? Rosters { get; private set; }
    public bool IsLoaded => Rosters is not null && Rosters.Count > 0;

    public async Task SetRosters(string league_id, bool forceRefresh = false)
    {
        if (!IsLoaded || forceRefresh)
        {
            var rosters = await _sleeperApi.GetRostersForLeagueAsync(league_id);
            if (rosters is {Count: > 0})
            {
                Rosters = rosters;
            }
        }
    }
    
    public async Task<RostersModel?> GetByRosterId(int roster_id)
    {
        if (roster_id < 0) return null;
        await EnsureLoadedAsync();
        return Rosters?.FirstOrDefault(r => r.RosterId == roster_id);
    }
        

    public async Task<RostersModel?> GetByUserId(string user_id)
    {
        if (string.IsNullOrWhiteSpace(user_id)) return null;
        await EnsureLoadedAsync();
        return Rosters?.FirstOrDefault(r => r.OwnerId == user_id);
    }

    public async Task<string> GetPlayerNickname(string player_id, string roster_id)
    {
        if (string.IsNullOrWhiteSpace(player_id) || string.IsNullOrWhiteSpace(roster_id)) return "";
        await EnsureLoadedAsync();
        var nickname = Rosters?.FirstOrDefault(r => r.RosterId.ToString() == roster_id)?.Metadata?.TryGetValue($"p_nick_{player_id}", out var nicknameValue) == true
            ? nicknameValue
            : "";
        return nickname;
    }
        
    private async Task EnsureLoadedAsync(bool forceRefresh = false)
    {
        if (IsLoaded && !forceRefresh) return;
        
        var leagueId = await _leagueState.GetCurrentLeagueId();
        if (string.IsNullOrWhiteSpace(leagueId))
        {
            throw new InvalidOperationException("Current league id is not available.");
        }
        await SetRosters(leagueId, true);
    }
    
}
