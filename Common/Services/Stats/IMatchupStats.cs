using Shared.Models;

namespace Shared.Services;

public interface IMatchupStats
{
    IReadOnlyList<KeyValuePair<MatchupModel, double>> GetLargestMarginOfVictory();
    IReadOnlyList<KeyValuePair<MatchupModel, double>> GetTightestMarginOfVictory();
    IReadOnlyDictionary<string, SeasonRecord> GetRecordsBySeason();
    IReadOnlyList<(string,Dictionary<int,int>)> GetEndOfSeasonFinishes();
    IReadOnlyDictionary<int, int> GetLastPlaceFinishesByRosterId();
    IReadOnlyDictionary<int, int> GetMostLossesAgainstRosterId();
    IReadOnlyDictionary<int, int> GetMostWinsAgainstRosterId();
    IReadOnlyList<MatchupModel> GetTopScoringPlayers();
    IReadOnlyList<MatchupModel> GetLowScoringPlayers();
}

public sealed class MatchupStats(LeagueData leagueData, MatchupData matchupData) : IMatchupStats
{

    public IReadOnlyList<KeyValuePair<MatchupModel, double>> GetLargestMarginOfVictory()
    {
        List<KeyValuePair<MatchupModel, double>> largestMarginOfVictory = new();
        var groupedMatchups = (matchupData.AllMatchups ?? Enumerable.Empty<MatchupModel>())
            .Where(m => m.LeagueId is not null && m.Week is not null && m.MatchupId is not null)
            .GroupBy(m => new { m.LeagueId, m.Week, m.MatchupId });

        var winnersWithMargin = new List<KeyValuePair<MatchupModel, double>>();

        foreach (var group in groupedMatchups)
        {
            var ordered = group.OrderByDescending(m => m.Points).ToList();
            if (ordered.Count == 0) continue;

            var topPoints = ordered[0].Points;
            var secondPoints = ordered.Count > 1 ? ordered[1].Points : 0;
            var margin = topPoints - secondPoints;

            foreach (var winner in ordered.Where(m => m.Points == topPoints))
            {
                winnersWithMargin.Add(new KeyValuePair<MatchupModel, double>(winner, margin));
            }
        }

        largestMarginOfVictory = winnersWithMargin.OrderByDescending(x => x.Value).ToList();
        return largestMarginOfVictory;
    }


    public IReadOnlyList<KeyValuePair<MatchupModel, double>> GetTightestMarginOfVictory()
    {
        List<KeyValuePair<MatchupModel, double>> tightestMarginOfVictory = new();
        var groupedMatchups = (matchupData.AllMatchups ?? Enumerable.Empty<MatchupModel>())
            .Where(m => m.LeagueId is not null && m.Week is not null && m.MatchupId is not null)
            .GroupBy(m => new { m.LeagueId, m.Week, m.MatchupId });

        var winnersWithMargin = new List<KeyValuePair<MatchupModel, double>>();

        foreach (var group in groupedMatchups)
        {
            var ordered = group.OrderByDescending(m => m.Points).ToList();
            if (ordered.Count == 0) continue;

            var topPoints = ordered[0].Points;
            var secondPoints = ordered.Count > 1 ? ordered[1].Points : 0;
            var margin = topPoints - secondPoints;

            foreach (var winner in ordered.Where(m => m.Points == topPoints))
            {
                winnersWithMargin.Add(new KeyValuePair<MatchupModel, double>(winner, margin));
            }
        }

        tightestMarginOfVictory = winnersWithMargin.OrderBy(x => x.Value).ToList();
        return tightestMarginOfVictory;
    }


    public IReadOnlyDictionary<string, SeasonRecord> GetRecordsBySeason()
    {
        Dictionary<string, SeasonRecord> recordsBySeason = new();
        if(matchupData.AllMatchups is null || matchupData.AllMatchups.Count == 0) return recordsBySeason;

        var seasonGroups = matchupData.AllMatchups 
            .Where(m => !string.IsNullOrWhiteSpace(m.Season) && !string.IsNullOrWhiteSpace(m.Week))
            .GroupBy(m => m.Season!);

        foreach (var seasonGroup in seasonGroups)
        {
            var season = seasonGroup.Key;
            var weekGroups = seasonGroup
                .Select(m => new { Matchup = m, WeekNumber = int.TryParse(m.Week, out var num) ? num : -1 })
                .Where(x => x.WeekNumber != -1)
                .GroupBy(x => x.WeekNumber!)
                .OrderBy(g => g.Key);
            var cumulative = new Dictionary<int, Record>();
            var cumulativePoints = new Dictionary<int, double>();
            var seasonRecord = new SeasonRecord(season);

            foreach (var weekGroup in weekGroups)
            {
                var matchupsThisWeek = weekGroup.Select(x => x.Matchup);
                foreach (var matchupPartition in matchupsThisWeek.GroupBy(m => m.MatchupId))
                {
                    var winnerRosterId = StatsData.GetWinnerRosterId(matchupPartition);
                    foreach (var team in matchupPartition)
                    {
                        if (!cumulative.TryGetValue(team.RosterId, out var current))
                        {
                            current = new Record();
                            cumulative[team.RosterId] = current;
                        }
                        if (!cumulativePoints.TryGetValue(team.RosterId, out var totalPoints))
                        {
                            totalPoints = 0;
                        }

                        totalPoints += team.Points;
                        cumulativePoints[team.RosterId] = totalPoints;
                        current.PointsFor = totalPoints.ToString("0.00");

                        if (!winnerRosterId.HasValue)
                        {
                            current.Ties += 1;
                        }
                        else if (winnerRosterId.Value == team.RosterId)
                        {
                            current.Wins += 1;
                        }
                        else
                        {
                            current.Losses += 1;
                        }
                    }
                }

                var weekRecord = new WeekRecord(weekGroup.Key);
                foreach (var entry in cumulative)
                {
                    weekRecord.ByRoster[entry.Key] = new Record
                    {
                        Wins = entry.Value.Wins,
                        Losses = entry.Value.Losses,
                        Ties = entry.Value.Ties,
                        PointsFor = entry.Value.PointsFor
                    };
                }

                seasonRecord.WeeksByNumber[weekGroup.Key] = weekRecord;
            }

            recordsBySeason[season] = seasonRecord;

        }

        return recordsBySeason;
    }


    public IReadOnlyList<(string,Dictionary<int,int>)> GetEndOfSeasonFinishes()
    {
        List<(string, Dictionary<int,int>)> endOfSeasonFinishes = new();

        var playoffStartBySeason = leagueData.AllLeagues
            .Where(l => !string.IsNullOrWhiteSpace(l.Season) && l.Settings?.PlayoffWeekStart is int)
            .GroupBy(l => l.Season!)
            .ToDictionary(g => g.Key, g => g.First().Settings!.PlayoffWeekStart);

        var recordsBySeason = GetRecordsBySeason();
        foreach (var seasonEntry in recordsBySeason)
        {
            var season = seasonEntry.Key;
            var seasonRecord = seasonEntry.Value;

            if (!playoffStartBySeason.TryGetValue(season, out var playoffStart) || playoffStart <= 0)
            {
                continue;
            }

            var latestWeek = seasonRecord.WeeksByNumber.Keys
                .Where(w => w < playoffStart)
                .DefaultIfEmpty(0)
                .Max();

            if (latestWeek == 0 || !seasonRecord.WeeksByNumber.TryGetValue(latestWeek, out var weekRecord))
            {
                continue;
            }

            var ordered = weekRecord.ByRoster
                .Select(kv =>
                {
                    var pointsFor = double.TryParse(
                        kv.Value.PointsFor,
                        out var pf)
                        ? pf
                        : double.MinValue;

                    return new
                    {
                        RosterId = kv.Key,
                        Record = kv.Value,
                        PointsFor = pointsFor
                    };
                })
                .OrderByDescending(x => x.Record.Wins)
                .ThenByDescending(x => x.PointsFor)
                .ToList();

            var rankByRoster = new Dictionary<int, int>();
            var rank = 1;

            foreach (var entry in ordered)
            {
                rankByRoster[rank] = entry.RosterId;
                rank++;
            }

            endOfSeasonFinishes.Add((season, rankByRoster));
        }

        return endOfSeasonFinishes;
    }

    
    public IReadOnlyDictionary<int, int> GetLastPlaceFinishesByRosterId()
    {
        Dictionary<int, int> lastPlaceFinishesByRosterId = new ()
        {
            { 1, 0 },
            { 2, 0 },
            { 3, 0 },
            { 4, 2 },
            { 5, 0 },
            { 6, 0 },
            { 7, 1 },
            { 8, 1 },
            { 9, 0 },
            { 10, 0}
        };

        var playoffStartByLeagueId = leagueData.AllLeagues
            .Where(l => l.LeagueId is not null && l.Settings?.PlayoffWeekStart is int)
            .ToDictionary(l => l.LeagueId!, l => l.Settings!.PlayoffWeekStart);

        var records = GetRecordsBySeason() ?? new Dictionary<string, SeasonRecord>();

        foreach (var seasonRecord in records.Values)
        {
            foreach (var weekRecord in seasonRecord.WeeksByNumber.Values)
            {
                var leagueId = leagueData.AllLeagues.FirstOrDefault(l => l.Season == seasonRecord.Season)?.LeagueId;
                if (leagueId is null || !playoffStartByLeagueId.TryGetValue(leagueId, out var playoffWeekStart))
                {
                    continue;
                }

                if (weekRecord.WeekNumber >= playoffWeekStart)
                {
                    continue;
                }
                
                if(weekRecord.WeekNumber == playoffWeekStart -1)
                {
                    var bottomEntry = weekRecord.ByRoster
                                    .OrderBy(kv => kv.Value.Wins)
                                    .ThenBy(kv => double.TryParse(kv.Value.PointsFor, out var pf)
                                        ? pf
                                        : double.MaxValue)
                                    .First();


                    lastPlaceFinishesByRosterId[bottomEntry.Key] = lastPlaceFinishesByRosterId.TryGetValue(bottomEntry.Key, out var count)
                    ? count + 1
                    : 1;
                }
            }
        }

        return lastPlaceFinishesByRosterId;
    }


    public IReadOnlyDictionary<int, int> GetMostLossesAgainstRosterId()
    {
        Dictionary<int, int> mostLossesAgainstRosterId = new();

        var lossesByRoster = new Dictionary<int, Dictionary<int, int>>();
        var winsByRoster = new Dictionary<int, Dictionary<int, int>>();

        var groupedMatchups = (matchupData.AllMatchups ?? Enumerable.Empty<MatchupModel>())
            .Where(m => m.MatchupId is not null)
            .GroupBy(m => (m.LeagueId, m.Season, m.Week, m.MatchupId));

        foreach (var group in groupedMatchups)
        {
            var teams = group.ToList();
            if (teams.Count < 2) continue;

            var winnerId = StatsData.GetWinnerRosterId(teams);
            if (!winnerId.HasValue) continue;

            foreach (var team in teams)
            {
                if (team.RosterId == winnerId.Value) continue;

                if (!lossesByRoster.TryGetValue(team.RosterId, out var opponentCounts))
                {
                    opponentCounts = new Dictionary<int, int>();
                    lossesByRoster[team.RosterId] = opponentCounts;
                }

                opponentCounts[winnerId.Value] = opponentCounts.TryGetValue(winnerId.Value, out var count)
                    ? count + 1
                    : 1;
            }

            var winnerOpponents = teams.Where(t => t.RosterId != winnerId.Value).ToList();
            if (winnerOpponents.Count > 0)
            {
                if (!winsByRoster.TryGetValue(winnerId.Value, out var opponentCounts))
                {
                    opponentCounts = new Dictionary<int, int>();
                    winsByRoster[winnerId.Value] = opponentCounts;
                }

                foreach (var loser in winnerOpponents)
                {
                    opponentCounts[loser.RosterId] = opponentCounts.TryGetValue(loser.RosterId, out var count)
                        ? count + 1
                        : 1;
                }
            }
        }

        foreach (var roster in lossesByRoster)
        {
            var mostLossesOpponent = roster.Value
                .OrderByDescending(kv => kv.Value)
                .First().Key;

            mostLossesAgainstRosterId[roster.Key] = mostLossesOpponent;
        }

        return mostLossesAgainstRosterId;
    }


    public IReadOnlyDictionary<int, int> GetMostWinsAgainstRosterId()
    {
        Dictionary<int, int> mostWinsAgainstRosterId = new();

        var lossesByRoster = new Dictionary<int, Dictionary<int, int>>();
        var winsByRoster = new Dictionary<int, Dictionary<int, int>>();

        var groupedMatchups = (matchupData.AllMatchups ?? Enumerable.Empty<MatchupModel>())
            .Where(m => m.MatchupId is not null)
            .GroupBy(m => (m.LeagueId, m.Season, m.Week, m.MatchupId));

        foreach (var group in groupedMatchups)
        {
            var teams = group.ToList();
            if (teams.Count < 2) continue;

            var winnerId = StatsData.GetWinnerRosterId(teams);
            if (!winnerId.HasValue) continue;

            foreach (var team in teams)
            {
                if (team.RosterId == winnerId.Value) continue;

                if (!lossesByRoster.TryGetValue(team.RosterId, out var opponentCounts))
                {
                    opponentCounts = new Dictionary<int, int>();
                    lossesByRoster[team.RosterId] = opponentCounts;
                }

                opponentCounts[winnerId.Value] = opponentCounts.TryGetValue(winnerId.Value, out var count)
                    ? count + 1
                    : 1;
            }

            var winnerOpponents = teams.Where(t => t.RosterId != winnerId.Value).ToList();
            if (winnerOpponents.Count > 0)
            {
                if (!winsByRoster.TryGetValue(winnerId.Value, out var opponentCounts))
                {
                    opponentCounts = new Dictionary<int, int>();
                    winsByRoster[winnerId.Value] = opponentCounts;
                }

                foreach (var loser in winnerOpponents)
                {
                    opponentCounts[loser.RosterId] = opponentCounts.TryGetValue(loser.RosterId, out var count)
                        ? count + 1
                        : 1;
                }
            }
        }

        foreach (var roster in winsByRoster)
        {
            var mostWinsOpponent = roster.Value
                .OrderByDescending(kv => kv.Value)
                .First().Key;

            mostWinsAgainstRosterId[roster.Key] = mostWinsOpponent;
        }

        return mostWinsAgainstRosterId;
    }


    public IReadOnlyList<MatchupModel> GetTopScoringPlayers()
    {
        return matchupData.AllMatchups ?? Enumerable.Empty<MatchupModel>()
                        .Select(m => new
                            {
                                Matchup = m,
                                TopStarterPoints = (m.Starters is null || m.PlayersPoints is null)
                                    ? 0
                                    : m.PlayersPoints
                                        .Where(kv => m.Starters.Contains(kv.Key))
                                        .Select(kv => kv.Value)
                                        .DefaultIfEmpty(0)
                                        .Max()
                            })
                        .OrderByDescending(x => x.TopStarterPoints)
                        .Select(x => x.Matchup)
                        .ToList();
    }


    public IReadOnlyList<MatchupModel> GetLowScoringPlayers()
    {
        return matchupData.AllMatchups ?? Enumerable.Empty<MatchupModel>()
                        .Select(m => new
                            {
                                Matchup = m,
                                LowestStarterPoints = (m.Starters is null || m.PlayersPoints is null)
                                    ? 0
                                    : m.PlayersPoints
                                        .Where(kv => m.Starters.Contains(kv.Key))
                                        .Select(kv => kv.Value)
                                        .DefaultIfEmpty(0)
                                        .Min()
                            })
                        .OrderBy(x => x.LowestStarterPoints)
                        .Select(x => x.Matchup)
                        .ToList();
    }
}