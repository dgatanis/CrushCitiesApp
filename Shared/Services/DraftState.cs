using Shared.Models;

namespace Shared.Services;

public sealed class DraftState(ISleeperAPI sleeperApi, LeagueState leagueState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;
    private Task? _loadTask;
    private Task? _cacheTask;
    
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
    /// Ensures DraftHistory loaded
    /// </summary>
    public bool IsCacheLoaded => DraftHistory is not null && DraftHistory.Count > 0;

    

    /// <summary>
    /// Ensures AllDrafts and AllDraftPicks is loaded
    /// </summary>
    public bool IsLoaded => AllDrafts is not null && AllDrafts.Count > 0 && AllDraftPicks is not null && AllDraftPicks.Count > 0;



    /// <summary>
    /// Sets the draft data for the given leagueid
    /// TODO: Change so it sets all Draft Data 
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task SetAllDraftDataAsync(bool forceRefresh = false)
    {
        try
        {
            if (!IsLoaded || forceRefresh)
            {
                await _leagueState.EnsureLoadedAsync();
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
        catch (Exception ex)
        {
            _loadTask = null;
            Console.WriteLine($"ERROR: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds dictionaries to be used for quicker lookups on pages
    /// </summary>
    /// <returns></returns>
    private async Task BuildLookupCachesAsync()
    {
        try
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
        catch (Exception ex)
        {
            _cacheTask = null;
            Console.WriteLine($"ERROR: {ex.Message}");
            throw;
        }
    }

    
    /// <summary>
    /// Ensures that the AllTransactions data is loaded.
    /// </summary>
    /// <returns></returns>
    public Task EnsureLoadedAsync()
    {
        if (IsLoaded) return Task.CompletedTask;
        _loadTask ??= SetAllDraftDataAsync(forceRefresh: true);
        return _loadTask;
    }


    /// <summary>
    /// Ensures the cached data is loaded.
    /// </summary>
    /// <returns></returns>
    public Task EnsureCacheLoadedAsync()
    {
        if (IsCacheLoaded) return Task.CompletedTask;
        _cacheTask ??= BuildLookupCachesAsync();
        return _cacheTask;
    }
}
