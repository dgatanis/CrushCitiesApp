using Shared.Models;

namespace Shared.Services;

public sealed class RosterState
{
    private readonly ISleeperAPI _sleeperApi;

    public RosterState(ISleeperAPI sleeperApi)
    {
        _sleeperApi = sleeperApi;
    }
    public List<RostersModel>? Rosters { get; private set; }
    public bool IsLoaded => Rosters is not null && Rosters.Count > 0;

    public async Task SetRosters(string league_id, bool forceRefresh = false)
    {
        if (!IsLoaded && !forceRefresh)
        {
            var rosters = await _sleeperApi.GetRostersForLeagueAsync(league_id);
            if (rosters is {Count: > 0})
            {
                Rosters = rosters;
            }
        }
    }
    
    public RostersModel? GetById(int roster_id) => 
        Rosters?.FirstOrDefault(r => r.RosterId == roster_id);
}
