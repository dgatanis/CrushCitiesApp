using Shared.Models;

namespace Shared.Services;

public sealed class RosterState(ISleeperAPI sleeperApi, UserState userState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly UserState _userState = userState;

    /// <summary>
    /// List of all rosters. 
    /// Set via SetRostersAsync(league_id)
    /// </summary>
    public List<RostersModel>? Rosters { get; private set; }

    /// <summary>
    /// Lookup dictionary for getting the team name by providing the roster_id.
    /// Set via BuildLookupCachesAsync()
    /// </summary>
    public readonly Dictionary<int, string> TeamNameByRosterId = new();

    /// <summary>
    /// Lookup dictionary for getting the player nickname by providing the roster_id and player_id.
    /// Set via BuildLookupCachesAsync()
    /// </summary>
    public readonly Dictionary<(int rosterId, string playerId), string> PlayerNicknameByRosterId = new();

    /// <summary>
    /// Lookup dictionary for getting the user_id (owner_id) by providing the roster_id.
    /// Set via BuildLookupCachesAsync()
    /// </summary>
    public readonly Dictionary<string, int> RosterIdByUserId = new();

    /// <summary>
    /// Lookup dictionary for getting a user_id by providing the roster_id.
    /// Set via BuildLookupCachesAsync()
    /// </summary>
    public readonly Dictionary<string, string> UserIdByRosterId = new();

    /// <summary>
    /// Ensures the cached lookups are loaded
    /// </summary>
    public bool CacheLoaded => PlayerNicknameByRosterId.Count > 0 && TeamNameByRosterId.Count > 0 && RosterIdByUserId.Count > 0 && UserIdByRosterId.Count > 0;
    
    /// <summary>
    /// Esnures the rosters are loaded
    /// </summary>
    public bool IsLoaded => Rosters is not null && Rosters.Count > 0;
    


    /// <summary>
    /// Sets the roster based on the leagueid
    /// </summary>
    /// <param name="league_id"></param>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task SetRostersAsync(string league_id, bool forceRefresh = false)
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
        UserIdByRosterId.Clear();

         var userIds = (_userState.Users ?? [])
            .Where(u => !string.IsNullOrWhiteSpace(u?.UserId))
            .Select(u => u!.UserId)
            .ToHashSet();

        // Team names by RosterId
        foreach (var roster in Rosters ?? [])
        {
            var teamName = $"Roster {roster.RosterId}";

            if (!string.IsNullOrWhiteSpace(roster.OwnerId))
            {
                var fromUser = await _userState.GetTeamNameByUserIdAsync(roster.OwnerId);
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

        // Get user_id by roster_id
        foreach (var roster in Rosters ?? [])
        {
            if (string.IsNullOrWhiteSpace(roster?.OwnerId)) continue;
            if (!userIds.Contains(roster.OwnerId)) continue;

            UserIdByRosterId[roster.RosterId.ToString()] = roster.OwnerId;
        }
    }
    
}
