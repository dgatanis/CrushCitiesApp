using Shared.Models;

namespace Shared.Services;

public sealed class DraftState(ISleeperAPI sleeperApi, LeagueState leagueState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;
    public List<DraftsModel>? Drafts { get; private set; }
    public DraftsModel? Draft { get; private set; }
    public List<DraftPicksModel>? DraftPicks { get; private set; }
    public bool IsLoaded => Drafts is not null && DraftPicks is not null;

    public async Task SetDraftData(string league_id, bool forceRefresh = false)
    {
        if (!IsLoaded || forceRefresh)
        {
            var drafts = await _sleeperApi.GetDraftsForLeague(league_id);
            if (drafts is { Count: > 0 })
            {
                Drafts = drafts;
                Draft = drafts.FirstOrDefault();

                var draftId = Draft?.DraftId;
                if (!string.IsNullOrWhiteSpace(draftId))
                {
                    var picks = await _sleeperApi.GetDraftPicksForDraft(draftId);
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

    private async Task EnsureLoadedAsync(bool forceRefresh = false)
    {
        if (IsLoaded && !forceRefresh) return;

        var leagueId = await _leagueState.GetCurrentLeagueId();
        if (string.IsNullOrWhiteSpace(leagueId))
        {
            throw new InvalidOperationException("Current league id is not available.");
        }
        await SetDraftData(leagueId, forceRefresh);
    }
}
