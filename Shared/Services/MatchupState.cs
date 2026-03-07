using Shared.Models;

namespace Shared.Services;

public sealed class MatchupState(ISleeperAPI sleeperApi)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    public List<MatchupModel>? Matchups { get; set; }
    public List<MatchupModel> AllMatchups { get; set; } = [];
    public bool IsLoaded => Matchups is not null && Matchups.Count > 0;
    public string? LeagueId { get; set; } = null;
    public string? Season { get; set; }

    public async Task SetMatchups(string league_id, string week, bool forceRefresh = false)
    {
        if (!IsLoaded || forceRefresh)
        {
            var matchups = await _sleeperApi.GetMatchupsForWeek(league_id, week);

            if (LeagueId != league_id)
            {
                var league = await _sleeperApi.GetLeagueAsync(league_id);
                Season = league.Season;
                LeagueId = league_id;
            }

            if (matchups is not null)
            {
                Matchups = matchups
                    .Select(m =>
                    {
                        m.Season = Season;
                        m.Week = week;
                        return m;
                    })
                    .OrderBy(m => m.MatchupId).ToList();

                AllMatchups.AddRange(matchups
                    .Select(m =>
                    {
                        m.Season = Season;
                        m.Week = week;
                        return m;
                    }).OrderBy(m => m.MatchupId).ToList());
            }
        }
    }

    public void ClearAllMatchups() => AllMatchups = null;
}
