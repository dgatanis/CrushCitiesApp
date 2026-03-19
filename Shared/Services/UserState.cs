using Shared.Models;

namespace Shared.Services;

public sealed class UserState(ISleeperAPI sleeperApi, LeagueState leagueState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;

    
    public List<UsersModel>? Users { get; private set; } 
    public readonly Dictionary<string, string> TeamNameByUserId = new();
    public readonly Dictionary<string, string> OwnerAvatarByUserId = new();
    public bool CacheLoaded => OwnerAvatarByUserId.Count > 0 && TeamNameByUserId.Count > 0;
    public bool IsLoaded => Users is not null && Users.Count > 0;


    /// <summary>
    /// Sets the users for the given leagueid and builds cache lookups 
    /// </summary>
    /// <param name="league_id"></param>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task SetUsersAsync(string league_id, bool forceRefresh = false)
    {
        if (!IsLoaded || forceRefresh)
        {
            var users = await _sleeperApi.GetUsersForLeagueAsync(league_id);
            if (users is not null)
            {
                Users = users;
            }
            await BuildLookupCachesAsync();
        }
    }


    /// <summary>
    /// Helper method to get the team name by userid 
    /// </summary>
    /// <param name="user_id"></param>
    /// <returns></returns>
    public async Task<string> GetTeamNameByUserIdAsync(string user_id) 
    {
        if (string.IsNullOrWhiteSpace(user_id)) return "";
        await EnsureLoadedAsync();
        var user = Users?.FirstOrDefault(r => r.UserId == user_id);
        var teamName = user?.Metadata?.TeamName;
        if(user is not null && !string.IsNullOrWhiteSpace(teamName)) return teamName;
        return "";
    }   


    /// <summary>
    /// Ensures the data is loaded, if not load it
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task EnsureLoadedAsync(bool forceRefresh = false)
    {
        if (IsLoaded && !forceRefresh) return;

            var leagueId = await _leagueState.GetCurrentLeagueIdAsync();
            if (string.IsNullOrWhiteSpace(leagueId))
            {
                throw new InvalidOperationException("Current league id is not available.");
            }
            await SetUsersAsync(leagueId, forceRefresh);
    }


    /// <summary>
    /// Builds dictionaries to be used for quicker lookups on pages
    /// </summary>
    /// <returns></returns>
    public async Task BuildLookupCachesAsync()
    {
        TeamNameByUserId.Clear();
        OwnerAvatarByUserId.Clear();

        //Get teamname by userid
        foreach (var user in Users ?? [])
        {
            var teamName = $"Roster {user.UserId}";

            if (!string.IsNullOrWhiteSpace(user.UserId))
            {
                var fromUser = await GetTeamNameByUserIdAsync(user.UserId);
                if (!string.IsNullOrWhiteSpace(fromUser))
                    teamName = fromUser;

                TeamNameByUserId[user.UserId] = teamName;
            }
        }

        //Get owner avatar by userid
        foreach (var user in Users ?? [])
        {
            if (string.IsNullOrWhiteSpace(user.UserId)) continue;

            if (!string.IsNullOrWhiteSpace(user.Metadata?.Avatar))
            {
                OwnerAvatarByUserId[user.UserId] = user.Metadata.Avatar;
            }
            else if (!string.IsNullOrWhiteSpace(user.Avatar))
            {
                OwnerAvatarByUserId[user.UserId] = $"https://sleepercdn.com/avatars/thumbs/{user.Avatar}";
            }
            else
            {
                OwnerAvatarByUserId[user.UserId] = "/images/question-mark.png";
            }
        }
    }
}
