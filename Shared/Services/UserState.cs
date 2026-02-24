using Shared.Models;

namespace Shared.Services;

public sealed class UserState(ISleeperAPI sleeperApi)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;

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

    public UsersModel? GetByUserId(string user_id) => 
        Users?.FirstOrDefault(r => r.UserId == user_id);
}
