using Shared.Models;

namespace Shared.Services;

public sealed class MatchupState(ISleeperAPI sleeperApi)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    public List<MatchupModel>? Matchups { get; set; } 
    public bool IsLoaded => Matchups is not null && Matchups.Count > 0;

    public async Task SetMatchups(string league_id, string week, bool forceRefresh = false)
    {
        if (!IsLoaded || forceRefresh)
        {
            var matchups = await _sleeperApi.GetMatchupsForWeek(league_id, week);
            if (matchups is not null)
            {
                Matchups = matchups.OrderBy(m => m.MatchupId).ToList();
            }
        }
    }
}
