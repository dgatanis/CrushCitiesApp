using Shared.Models;

namespace Shared.Services;

public sealed class UserState(ISleeperAPI sleeperApi, LeagueState leagueState, INormalizer normalizer)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;
    private readonly INormalizer _normalizer = normalizer;
    private Task? _loadTask;
    private Task? _cacheTask;
    private bool _dataLoaded = false;
    private bool _cacheLoaded = false;

    /// <summary>
    /// List of all users. Verify using EnsureLoadedAsync() before accessing.
    /// </summary>
    public List<UsersModel>? Users { get; private set; } 

    /// <summary>
    /// Lookup dictionary for getting a users team name by providing their user_id (owner_id).
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public readonly Dictionary<string, string> TeamNameByUserId = new();

    /// <summary>
    /// Lookup dictionary for getting a users avatar by providing the user_id (owner_id).
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public readonly Dictionary<string, string> OwnerAvatarByUserId = new();

    /// <summary>
    /// Ensures the lookups are loaded
    /// </summary>
    private bool IsCacheLoaded => _cacheLoaded;

    /// <summary>
    /// Ensures the Users data is loaded
    /// </summary>
    private bool IsLoaded => _dataLoaded;


    /// <summary>
    /// Sets the users for the given current leagueId and builds cache lookups 
    /// </summary>
    /// <param name="league_id"></param>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    private async Task SetCurrentUsersAsync(bool forceRefresh = false)
    {
        try
        {
            await _leagueState.EnsureLoadedAsync();
            var currentLeagueId = _leagueState.CurrentLeagueId;
            if (!IsLoaded || forceRefresh)
            {
                var users = await _sleeperApi.GetUsersForLeagueAsync(currentLeagueId);
                if (users is not null)
                {
                    Users = _normalizer.NormalizeUsers(users);
                }
            }

            _dataLoaded = true;
            await BuildLookupsAsync();
        }
        catch (Exception ex)
        {
            _loadTask = null;
            _dataLoaded = false;
            Console.WriteLine($"ERROR: {ex.Message}");
            throw;
        }
    }


    /// <summary>
    /// Helper method to get the team name by user_id (owner_id)
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
    /// Builds dictionaries to be used for quicker lookups on pages
    /// </summary>
    /// <returns></returns>
    private async Task BuildLookupsAsync()
    {
        try
        {
            await EnsureLoadedAsync();
            TeamNameByUserId.Clear();
            OwnerAvatarByUserId.Clear();

            // Get teamname by userid
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

            // Get owner avatar by userid
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
            _cacheLoaded = true;
        }
        catch (Exception ex)
        {
            _cacheTask = null;
            _cacheLoaded = false;
            Console.WriteLine($"ERROR: {ex.Message}");
            throw;
        }
    }


    /// <summary>
    /// Ensures the Users data is loaded.
    /// </summary>
    /// <returns></returns>
    public Task EnsureLoadedAsync()
    {
        if (IsLoaded) return Task.CompletedTask;
        _loadTask ??= SetCurrentUsersAsync(forceRefresh: true);
        return _loadTask;
    }


    /// <summary>
    /// Ensures the lookup data is loaded.
    /// </summary>
    /// <returns></returns>
    public Task EnsureLookupsLoadedAsync()
    {
        if (IsCacheLoaded) return Task.CompletedTask;
        _cacheTask ??= BuildLookupsAsync();
        return _cacheTask;
    }
}