using Shared.Models;

namespace Shared.Services;

public sealed class DraftState(ISleeperAPI sleeperApi)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;

    
    public List<DraftsModel>? Drafts { get; private set; }
    public DraftsModel? Draft { get; private set; }
    public List<DraftPicksModel>? DraftPicks { get; private set; }
    public bool IsLoaded => Drafts is not null && DraftPicks is not null;


    /// <summary>
    /// Sets the draft data for the given leagueid
    /// </summary>
    /// <param name="league_id"></param>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task SetDraftData(string league_id, bool forceRefresh = false)
    {
        if (!IsLoaded || forceRefresh)
        {
            var drafts = await _sleeperApi.GetDraftsForLeagueAsync(league_id);
            if (drafts is { Count: > 0 })
            {
                Drafts = drafts;
                Draft = drafts.FirstOrDefault();

                var draftId = Draft?.DraftId;
                if (!string.IsNullOrWhiteSpace(draftId))
                {
                    var picks = await _sleeperApi.GetDraftPicksForDraftAsync(draftId);
                    DraftPicks = picks?.Where(p => p is not null).Select(p => p!).ToList() ?? [];
                }
                else
                {
                    DraftPicks = [];
                }
            }
            else
            {
                Drafts = [];
                Draft = null;
                DraftPicks = [];
            }
        }
    }
}
