using Shared.Models;

namespace Shared.Services;

public sealed class UserState(ISleeperAPI sleeperApi, LeagueState leagueState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;
    public List<UsersModel>? Users { get; private set; } 
    public bool IsLoaded => Users is not null && Users.Count > 0;

    public async Task SetUsers(string league_id, bool forceRefresh = false)
    {
        if (!IsLoaded || forceRefresh)
        {
            var users = await _sleeperApi.GetUsersForLeagueAsync(league_id);
            if (users is not null)
            {
                Users = users;
            }
        }
    }

    public async Task<UsersModel?> GetByUserId(string user_id)
    {
        if (string.IsNullOrWhiteSpace(user_id)) return null;
        await EnsureLoadedAsync();
        return Users?.FirstOrDefault(r => r.UserId == user_id);

    }

    public async Task<string?> GetTeamNameByUserId(string user_id) 
    {
        if (string.IsNullOrWhiteSpace(user_id)) return null;
        await EnsureLoadedAsync();
        return Users?.FirstOrDefault(r => r.UserId == user_id)?.Metadata?.TeamName;
    }

    public async Task<string> GetOwnerAvatarImage(string user_id) 
    {
        if (string.IsNullOrWhiteSpace(user_id)) return "/images/question-mark.png";
        await EnsureLoadedAsync();
        if(Users?.FirstOrDefault(r => r.UserId == user_id)?.Metadata?.Avatar is not null)
        {
            return $"{Users?.FirstOrDefault(r => r.UserId == user_id)?.Metadata?.Avatar}";
        }   
        else
        {
            return $"https://sleepercdn.com/avatars/thumbs/{Users?.FirstOrDefault(r => r.UserId == user_id)?.Avatar}";
        }
    }

    private async Task EnsureLoadedAsync(bool forceRefresh = false)
    {
        if (IsLoaded && !forceRefresh) return;

            var leagueId = await _leagueState.GetCurrentLeagueId();
            if (string.IsNullOrWhiteSpace(leagueId))
            {
                throw new InvalidOperationException("Current league id is not available.");
            }
            await SetUsers(leagueId, forceRefresh);
    }

}
