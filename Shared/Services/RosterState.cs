using Shared.Models;

namespace Shared.Services;

public sealed class RosterState(ISleeperAPI sleeperApi, UserState userState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly UserState _userState = userState;

    
    public List<RostersModel>? Rosters { get; private set; }
    public bool IsLoaded => Rosters is not null && Rosters.Count > 0;
    public readonly Dictionary<int, string> TeamNameByRosterId = new();
    public readonly Dictionary<(int rosterId, string playerId), string> PlayerNicknameByRosterId = new();
    public readonly Dictionary<string, int> RosterIdByUserId = new();
    public bool CacheLoaded => PlayerNicknameByRosterId.Count > 0 && TeamNameByRosterId.Count > 0 && RosterIdByUserId.Count > 0;


    /// <summary>
    /// Sets the roster based on the leagueid
    /// </summary>
    /// <param name="league_id"></param>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
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
        await BuildLookupCachesAsync();
    }
        
    
    /// <summary>
    /// Builds dictionaries to be used for quicker lookups on pages
    /// </summary>
    /// <returns></returns>
    public async Task BuildLookupCachesAsync()
    {
        TeamNameByRosterId.Clear();
        PlayerNicknameByRosterId.Clear();
        RosterIdByUserId.Clear();

        // Team names by RosterId
        foreach (var roster in Rosters ?? [])
        {
            var teamName = $"Roster {roster.RosterId}";

            if (!string.IsNullOrWhiteSpace(roster.OwnerId))
            {
                var fromUser = await _userState.GetTeamNameByUserId(roster.OwnerId);
                if (!string.IsNullOrWhiteSpace(fromUser))
                    teamName = fromUser;
            }
            
            TeamNameByRosterId[roster.RosterId] = teamName;
        }


        //Player nicknames by rosterid
        foreach (var roster in Rosters ?? [])
        {
            if (roster.Metadata is null) continue;

            foreach (var kvp in roster.Metadata)
            {
                // keys look like "p_nick_<playerId>"
                if (!kvp.Key.StartsWith("p_nick_", StringComparison.Ordinal)) continue;

                var playerId = kvp.Key.Substring("p_nick_".Length);
                if (string.IsNullOrWhiteSpace(playerId)) continue;

                var nickname = kvp.Value ?? "";
                if (nickname.Length == 0) continue;

                PlayerNicknameByRosterId[(roster.RosterId, playerId)] = nickname;
            }
        }

        //Get RosterId by UserId
        foreach (var roster in Rosters ?? [])
        {
            if (string.IsNullOrWhiteSpace(roster.OwnerId)) continue;
            RosterIdByUserId[roster.OwnerId] = roster.RosterId;
        }
    }
    
}
