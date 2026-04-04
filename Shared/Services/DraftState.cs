using Shared.Models;

namespace Shared.Services;

public sealed class DraftState(ISleeperAPI sleeperApi, LeagueState leagueState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;
    private Task? _loadTask;
    private Task? _cacheTask;
    private bool _cacheLoaded = false;
    private bool _dataLoaded = false;
    
    /// <summary>
    /// List of all drafts for a league (DraftsModel). Verify using EnsureLoadedAsync() before accessing.
    /// </summary>
    public List<DraftsModel>? AllDrafts { get; private set; } = new();

    /// <summary>
    /// List of all draft picks for a league (DraftPicksModel). Verify using EnsureLoadedAsync() before accessing.
    /// </summary>
    public List<DraftPicksModel>? AllDraftPicks { get; private set; }

    /// <summary>
    /// Lookup list for finding quick details about a draft.
    /// Stores Season, LeagueId, DraftId and a mapping between roster_id and draft_slot for that draft.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public List<DraftPickSeasonSummary> DraftHistory { get; private set; } = new();

    /// <summary>
    /// Ensures lookups are loaded
    /// </summary>
    private bool IsCacheLoaded => _cacheLoaded;

    /// <summary>
    /// Ensures AllDrafts and AllDraftPicks is loaded
    /// </summary>
    private bool IsLoaded => _dataLoaded;



    /// <summary>
    /// Sets all draft data beginning with the CurrentLeagueId
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    private async Task SetAllDraftDataAsync(bool forceRefresh = false)
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
    /// Builds dictionaries to be used for quicker lookups on pages
    /// </summary>
    /// <returns></returns>
    private async Task BuildLookupsAsync()
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