using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Net;

namespace Shared.Services;

/// <summary>
/// Stats interface for getting a variety of stats about the league.
/// </summary>
public interface IStatsData
{
    /// <summary>
    /// List of MatchupModels of highest scoring teams ordered by points.
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyList<MatchupModel> GetTopScoringTeams();

    /// <summary>
    /// List of MatchupModels of lowest scoring teams ordered by points.
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyList<MatchupModel> GetLowScoringTeams();

    /// <summary>
    /// Dictionary of total weeks with highest score by roster id, grouped by season.
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyDictionary<string, Dictionary<int, int>> GetHighScoringWeeksByRosterId();

    /// <summary>
    /// Dictionary of total weeks with lowest score by roster id.
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyDictionary<int, int> GetLowScoringWeeksByRosterId();

    /// <summary>
    /// List of MatchupModels and margin for the largest margins of victory.
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyList<KeyValuePair<MatchupModel, double>> GetLargestMarginOfVictory();

    /// <summary>
    /// List of MatchupModels and margin for the tightest margins of victory.
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyList<KeyValuePair<MatchupModel, double>> GetTightestMarginOfVictory();

    /// <summary>
    /// Lookup dictionary for records per team, per season.
    /// Key is season and the value is a SeasonRecord. 
    /// SeasonRecord contains a dictionary with key of week number and a value of a WeekRecord which contains records per roster id.
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// <br />
    /// Example: 
    /// <c>
    /// RecordsBySeason["2024"].WeeksByNumber[9].ByRoster[1]
    /// </c>
    /// </summary>
    IReadOnlyDictionary<string, SeasonRecord> GetRecordsBySeason();

    /// <summary>
    /// Lookup list of season (key) and Dictionary(rank, roster_id).
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyList<(string,Dictionary<int,int>)> GetEndOfSeasonFinishes();

    /// <summary>
    /// Dictionary for championship count by roster id.
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyDictionary<int, int> GetChampionshipCountByRosterId();
    
    /// <summary>
    /// Dictionary for finals appearances by roster id.
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyDictionary<int, int> GetFinalsAppearancesByRosterId();

    /// <summary>
    /// Dictionary for finals appearances by roster id.
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyDictionary<int, int> GetPlayoffAppearancesByRosterId();

    /// <summary>
    /// Dictionary for last place finishes (at end of regular season) by roster id.
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyDictionary<int, int> GetLastPlaceFinishesByRosterId();

    /// <summary>
    /// Dictionary for most traded with by roster id (roster_id, roster_id they have traded with the most).
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyDictionary<int, int> GetMostTradedWithRosterId();

    /// <summary>
    /// Dictionary for trade count per roster_id.
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyDictionary<int, int> GetTradeCountsByRosterId();

    /// <summary>
    /// Dictionary for most losses against a roster_id (roster_id, roster_id they lost to the most).
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyDictionary<int, int> GetMostLossesAgainstRosterId();

    /// <summary>
    /// Dictionary for most wins against a roster_id (roster_id, roster_id they won against to the most).
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyDictionary<int, int> GetMostWinsAgainstRosterId();

    /// <summary>
    /// List of MatchupModels of highest scoring players order by player points.
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyList<MatchupModel> GetTopScoringPlayers();

    /// <summary>
    /// List of MatchupModels of lowest scoring players order by player points.
    /// Verify using EnsureDataLoadedAsync() before accessing.
    /// </summary>
    IReadOnlyList<MatchupModel> GetLowScoringPlayers();

    /// <summary>
    /// Sums the amount of high scoring weeks per roster_id.
    /// </summary>
    /// <param name="bySeason">IReadOnlyDictionary<season, Dictionary<roster_id, total_high_scoring_weeks>></param>
    /// <returns>Dictionary(roster_id,count)</returns>
    Dictionary<int, int> SumHighScoringWeeks(Dictionary<string, Dictionary<int, int>> weeks_by_season);

    /// <summary>
    /// Sums the records for each roster_id.
    /// </summary>
    /// <param name="season_records">Dictionary<season, SeasonRecord>. See StatsModel for more information on SeasonRecord.</param>
    /// <returns>Dictionary<roster_id, "W-L-T"></returns>
    Dictionary<int, string> SumRecords(Dictionary<string, SeasonRecord> season_records);

    /// <summary>
    /// Ensures the data required for stats is loaded. Always call this method before accessing StatsData.
    /// </summary>
    /// <returns></returns>
    Task EnsureDataLoadedAsync();
}

public class StatsData(IRosterStats rosterStats, IMatchupStats matchupStats, IPlayoffStats playoffStats,
                        ITransactionStats transactionStats, LeagueData leagueData, MatchupData matchupData, 
                        TransactionData transactionData, PlayoffData playoffData) : IStatsData
{
    private readonly IRosterStats _rosterStats = rosterStats;
    private readonly IMatchupStats _matchupStats = matchupStats;
    private readonly IPlayoffStats _playoffStats = playoffStats;
    private readonly ITransactionStats _transactionStats = transactionStats;
    private readonly LeagueData _leagueData = leagueData;
    private readonly MatchupData _matchupData = matchupData;
    private readonly TransactionData _transactionData = transactionData;
    private readonly PlayoffData _playoffData = playoffData;
    private Task? _loadTask;
    private bool _dataLoaded = false;
    public bool IsDataLoaded => _dataLoaded;

    // Roster Stats
    public IReadOnlyList<MatchupModel> GetTopScoringTeams() => _rosterStats.GetTopScoringTeams();
    public IReadOnlyList<MatchupModel> GetLowScoringTeams() => _rosterStats.GetLowScoringTeams();
    public IReadOnlyDictionary<string, Dictionary<int, int>> GetHighScoringWeeksByRosterId() => _rosterStats.GetHighScoringWeeksByRosterId();
    public IReadOnlyDictionary<int, int> GetLowScoringWeeksByRosterId() => _rosterStats.GetLowScoringWeeksByRosterId();

    // Matchup Stats
    public IReadOnlyList<KeyValuePair<MatchupModel, double>> GetLargestMarginOfVictory() => _matchupStats.GetLargestMarginOfVictory();
    public IReadOnlyList<KeyValuePair<MatchupModel, double>> GetTightestMarginOfVictory() => _matchupStats.GetTightestMarginOfVictory();
    public IReadOnlyDictionary<string, SeasonRecord> GetRecordsBySeason() => _matchupStats.GetRecordsBySeason();
    public IReadOnlyList<(string,Dictionary<int,int>)> GetEndOfSeasonFinishes() => _matchupStats.GetEndOfSeasonFinishes();
    public IReadOnlyDictionary<int, int> GetLastPlaceFinishesByRosterId() => _matchupStats.GetLastPlaceFinishesByRosterId();
    public IReadOnlyDictionary<int, int> GetMostLossesAgainstRosterId() => _matchupStats.GetMostLossesAgainstRosterId();
    public IReadOnlyDictionary<int, int> GetMostWinsAgainstRosterId() => _matchupStats.GetMostWinsAgainstRosterId();
    public IReadOnlyList<MatchupModel> GetTopScoringPlayers() => _matchupStats.GetTopScoringPlayers();
    public IReadOnlyList<MatchupModel> GetLowScoringPlayers() => _matchupStats.GetLowScoringPlayers();

    // Playoff Stats
    public IReadOnlyDictionary<int, int> GetChampionshipCountByRosterId() => _playoffStats.GetChampionshipCountByRosterId();
    public IReadOnlyDictionary<int, int> GetFinalsAppearancesByRosterId() => _playoffStats.GetFinalsAppearancesByRosterId();
    public IReadOnlyDictionary<int, int> GetPlayoffAppearancesByRosterId() => _playoffStats.GetPlayoffAppearancesByRosterId();

    // Transaction Stats
    public IReadOnlyDictionary<int, int> GetTradeCountsByRosterId() => _transactionStats.GetTradeCountsByRosterId();
    public IReadOnlyDictionary<int, int> GetMostTradedWithRosterId() => _transactionStats.GetMostTradedWithRosterId();
    

    public Dictionary<int, int> SumHighScoringWeeks(Dictionary<string, Dictionary<int, int>> weeks_by_season)
    {
        var totals = new Dictionary<int, int>();
        foreach (var season in weeks_by_season.Values)
        {
            foreach (var kv in season)
            {
                if (totals.TryGetValue(kv.Key, out var count))
                {
                    totals[kv.Key] = count + kv.Value;
                }
                else
                {
                    totals[kv.Key] = kv.Value;
                }
            }
        }
        return totals;
    }

    public static int? GetWinnerRosterId(IEnumerable<MatchupModel> teams)
    {
        var ordered = teams.OrderByDescending(t => t.Points).ToList();
        if (ordered.Count == 0)
        {
            return null;
        }

        var topPoints = ordered[0].Points;
        var topCount = ordered.Count(t => t.Points == topPoints);
        return topCount > 1 ? null : ordered[0].RosterId;
    }


    public Dictionary<int, string> SumRecords(Dictionary<string, SeasonRecord> season_records)
    {
        var totals = new Dictionary<int, Record>();

        var playoffStartBySeason = _leagueData.AllLeagues
            .Where(l => !string.IsNullOrWhiteSpace(l.Season) && l.Settings?.PlayoffWeekStart is int)
            .GroupBy(l => l.Season!)
            .ToDictionary(g => g.Key, g => g.First().Settings!.PlayoffWeekStart);

        foreach (var seasonEntry in season_records)
        {
            var season = seasonEntry.Key;
            var seasonRecord = seasonEntry.Value;

            if (!playoffStartBySeason.TryGetValue(season, out var playoffStart))
            {
                continue;
            }

            var cutoffWeek = playoffStart > 0 ? playoffStart - 1 : int.MaxValue;
            var latestWeek = seasonRecord.WeeksByNumber.Keys
                .Where(w => w <= cutoffWeek)
                .DefaultIfEmpty(0)
                .Max();

            if (latestWeek == 0) continue;

            if (!seasonRecord.WeeksByNumber.TryGetValue(latestWeek, out var weekRecord)) continue;

            foreach (var kv in weekRecord.ByRoster)
            {
                if (!totals.TryGetValue(kv.Key, out var record))
                {
                    record = new Record();
                    totals[kv.Key] = record;
                }

                record.Wins += kv.Value.Wins;
                record.Losses += kv.Value.Losses;
                record.Ties += kv.Value.Ties;
            }
        }

        var result = new Dictionary<int, string>();
        foreach (var kv in totals)
        {
            result[kv.Key] = kv.Value.Display;
        }

        return result;
    }

    /// <summary>
    /// Ensures the data is loaded that is required for Stat generation
    /// </summary>
    /// <param name="league_id"></param>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    private async Task EnsureRequiredDataLoadedAsync()
    {
        await _leagueData.EnsureLoadedAsync();
        await _transactionData.EnsureLoadedAsync();
        await _matchupData.EnsureLoadedAsync();
        await _playoffData.EnsureLoadedAsync();
        _dataLoaded = true;
    }


    public Task EnsureDataLoadedAsync()
    {
        if (IsDataLoaded) return Task.CompletedTask;
        _loadTask ??= EnsureRequiredDataLoadedAsync();
        return _loadTask;
    }
}