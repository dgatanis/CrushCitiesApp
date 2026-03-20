using Shared.Models;

namespace Shared.Services;

public sealed class DraftState(ISleeperAPI sleeperApi, LeagueState leagueState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;
    
    /// <summary>
    /// List of all drafts for a league (DraftsModel).
    /// Set via SetAllDraftDataAsync()
    /// </summary>
    public List<DraftsModel>? AllDrafts { get; private set; } = new();

    /// <summary>
    /// List of all draft picks for a league (DraftPicksModel).
    /// Set via SetAllDraftDataAsync()
    /// </summary>
    public List<DraftPicksModel>? AllDraftPicks { get; private set; }

    /// <summary>
    /// Lookup list for finding quick details about a draft.
    /// Stores Season, LeagueId, DraftId and a mapping between roster_id and draft_slot for that draft.
    /// Set via BuildLookupCachesAsync()
    /// </summary>
    public List<DraftPickSeasonSummary> DraftHistory { get; private set; } = new();

    /// <summary>
    /// Ensures AllDrafts and AllDraftPicks is loaded
    /// </summary>
    public bool IsLoadedAllDrafts => AllDrafts is not null && AllDraftPicks is not null;



    /// <summary>
    /// Sets the draft data for the given leagueid
    /// TODO: Change so it sets all Draft Data 
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task SetAllDraftDataAsync(bool forceRefresh = false)
    {
        if (!IsLoadedAllDrafts || forceRefresh)
        {
            if(!_leagueState.IsLoadedAllLeagues) await _leagueState.SetAllLeaguesDataAsync();
            AllDrafts = new();
            AllDraftPicks = new();

            foreach(var league in _leagueState.AllLeagues)
            {
                var drafts = await _sleeperApi.GetDraftsForLeagueAsync(league.LeagueId ?? "");

                if (drafts is { Count: > 0 })
                {
                    AllDrafts.AddRange(drafts.ToList());
                    var Draft = drafts.FirstOrDefault();

                    var draftId = Draft?.DraftId;
                    if (!string.IsNullOrWhiteSpace(draftId))
                    {
                        var picks = await _sleeperApi.GetDraftPicksForDraftAsync(draftId);
                        AllDraftPicks.AddRange(picks?.Where(p => p is not null).Select(p => p!).ToList() ?? []);
                    }
                }
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
        DraftHistory.Clear();

        // Draft History
        foreach(var draft in AllDrafts ?? [])
        {
            var slotToOwner = draft.DraftOrder?.ToDictionary(kvp => 
                                                            kvp.Value, kvp => kvp.Key)
                                                            ?? new Dictionary<int, string>();

            DraftHistory.Add(new DraftPickSeasonSummary
            {
                Season = draft.Season,
                LeagueId = draft.LeagueId,
                DraftId = draft.DraftId,
                DraftOrder = slotToOwner
            });
        }
    }
}
