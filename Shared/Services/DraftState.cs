using Microsoft.Extensions.Logging;
using Shared.Models;

namespace Shared.Services;


/// <summary>
/// Service that stores all-time draft information and builds lookups for frequently accessed data.
/// </summary>
/// <param name="sleeperApi"></param>
/// <param name="leagueState"></param>
/// <param name="logger"></param>
public sealed class DraftState(ISleeperAPI sleeperApi, LeagueState leagueState, ILogger<DraftState> logger)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;
    private readonly ILogger<DraftState> _logger = logger;
    private Task? _loadTask;
    private Task? _lookupTask;
    private bool _lookupsLoaded = false;
    private bool _dataLoaded = false;
    
    private List<DraftsModel>? _allDrafts = new List<DraftsModel>();
    private List<DraftPicksModel>? _allDraftPicks = new List<DraftPicksModel>();
    private List<DraftPickSeasonSummary> _draftHistory = new List<DraftPickSeasonSummary>();
    
    /// <summary>
    /// List of all drafts for a league (DraftsModel). Verify using EnsureLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<DraftsModel>? AllDrafts => _allDrafts;

    /// <summary>
    /// List of all draft picks for a league (DraftPicksModel). Verify using EnsureLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<DraftPicksModel>? AllDraftPicks => _allDraftPicks;

    /// <summary>
    /// Lookup list for finding quick details about a draft.
    /// Stores Season, LeagueId, DraftId and a mapping between roster_id and draft_slot for that draft.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<DraftPickSeasonSummary> DraftHistory => _draftHistory;

    /// <summary>
    /// Ensures lookups are loaded
    /// </summary>
    private bool IsLookupLoaded => _lookupsLoaded;

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
                _allDrafts = new();
                _allDraftPicks = new();

                foreach(var league in _leagueState.AllLeagues)
                {
                    var drafts = await _sleeperApi.GetDraftsForLeagueAsync(league.LeagueId ?? "");

                    if (drafts is { Count: > 0 })
                    {
                        _allDrafts.AddRange(drafts.ToList());
                        var Draft = drafts.FirstOrDefault();

                        var draftId = Draft?.DraftId;
                        if (!string.IsNullOrWhiteSpace(draftId))
                        {
                            var picks = await _sleeperApi.GetDraftPicksForDraftAsync(draftId);
                            _allDraftPicks?.AddRange(picks?.Where(p => p is not null).Select(p => p!).ToList() ?? []);
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
            _logger.LogError(ex, "ERROR: {Message}", ex.Message);
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
            _draftHistory.Clear();

            // Draft History
            foreach(var draft in AllDrafts ?? [])
            {
                var slotToOwner = draft.DraftOrder?.ToDictionary(kvp => 
                                                                kvp.Value, kvp => kvp.Key)
                                                                ?? new Dictionary<int, string>();

                _draftHistory.Add(new DraftPickSeasonSummary
                {
                    Season = draft.Season,
                    LeagueId = draft.LeagueId,
                    DraftId = draft.DraftId,
                    DraftOrder = slotToOwner
                });
            }
            _lookupsLoaded = true;
        }
        catch (Exception ex)
        {
            _lookupTask = null;
            _lookupsLoaded = false;
            _logger.LogError(ex, "ERROR: {Message}", ex.Message);
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
        if (IsLookupLoaded) return Task.CompletedTask;
        _lookupTask ??= BuildLookupsAsync();
        return _lookupTask;
    }
}

