using Shared.Models;

namespace Shared.Services;

public interface IRosterStats
{
    IReadOnlyList<MatchupModel> GetTopScoringTeams();
    IReadOnlyList<MatchupModel> GetLowScoringTeams();
    IReadOnlyDictionary<string, Dictionary<int, int>> GetHighScoringWeeksByRosterId();
    IReadOnlyDictionary<int, int> GetLowScoringWeeksByRosterId();
}

public sealed class RosterStats(LeagueData leagueData, MatchupData matchupData) : IRosterStats
{
    
    public IReadOnlyList<MatchupModel> GetTopScoringTeams()
    {
    return  matchupData.AllMatchups.OrderByDescending(m => m.Points).ToList();
    }


    public IReadOnlyList<MatchupModel> GetLowScoringTeams()
    {
        return  matchupData.AllMatchups.OrderBy(m => m.Points).ToList();
    }


    public IReadOnlyDictionary<string, Dictionary<int, int>> GetHighScoringWeeksByRosterId()
    {
        Dictionary<string, Dictionary<int, int>> highScoringWeeksByRosterId = new();

        var playoffStartByLeagueId = leagueData.AllLeagues
            .Where(l => l.LeagueId is not null && l.Settings?.PlayoffWeekStart is int)
            .ToDictionary(l => l.LeagueId!, l => l.Settings!.PlayoffWeekStart);
        var allRosterIds = (matchupData.AllMatchups ?? Enumerable.Empty<MatchupModel>())
            .Select(m => m.RosterId)
            .Distinct()
            .ToList();
        var seasons = (matchupData.AllMatchups ?? Enumerable.Empty<MatchupModel>())
            .Where(m => !string.IsNullOrWhiteSpace(m.Season))
            .Select(m => m.Season!)
            .Distinct()
            .ToList();
        var groupedByWeek = (matchupData.AllMatchups ?? Enumerable.Empty<MatchupModel>())
            .Where(m => m.LeagueId is not null && m.Week is not null && !string.IsNullOrWhiteSpace(m.Season))
            .GroupBy(m => new { m.LeagueId, m.Week, Season = m.Season! });

        foreach (var season in seasons)
        {
            if (!highScoringWeeksByRosterId.TryGetValue(season, out var rosterCounts))
            {
                rosterCounts = new Dictionary<int, int>();
                highScoringWeeksByRosterId[season] = rosterCounts;
            }

            foreach (var rosterId in allRosterIds)
            {
                if (!rosterCounts.ContainsKey(rosterId))
                {
                    rosterCounts[rosterId] = 0;
                }
            }
        }

        foreach (var group in groupedByWeek)
        {
            if (!playoffStartByLeagueId.TryGetValue(group.Key.LeagueId!, out var playoffWeekStart))
            {
                continue;
            }

            if (!int.TryParse(group.Key.Week, out var week) || week >= playoffWeekStart)
            {
                continue;
            }

            var maxPoints = group.Max(m => m.Points);
            foreach (var winner in group.Where(m => m.Points == maxPoints))
            {
                if (!highScoringWeeksByRosterId.TryGetValue(group.Key.Season, out var rosterCounts))
                {
                    rosterCounts = new Dictionary<int, int>();
                    highScoringWeeksByRosterId[group.Key.Season] = rosterCounts;
                }

                if (rosterCounts.TryGetValue(winner.RosterId, out var count))
                {
                    rosterCounts[winner.RosterId] = count + 1;
                }
                else
                {
                    rosterCounts[winner.RosterId] = 1;
                }
            }
        }

        return highScoringWeeksByRosterId;
    }


    public IReadOnlyDictionary<int, int> GetLowScoringWeeksByRosterId()
    {
        Dictionary<int, int> lowScoringWeeksByRosterId = new();
        var playoffStartByLeagueId = leagueData.AllLeagues
            .Where(l => l.LeagueId is not null && l.Settings?.PlayoffWeekStart is int)
            .ToDictionary(l => l.LeagueId!, l => l.Settings!.PlayoffWeekStart);

        var allRosterIds = (matchupData.AllMatchups ?? Enumerable.Empty<MatchupModel>())
            .Select(m => m.RosterId)
            .Distinct();

        foreach (var rosterId in allRosterIds)
        {
            lowScoringWeeksByRosterId[rosterId] = 0;
        }

        var groupedByWeek = (matchupData.AllMatchups ?? Enumerable.Empty<MatchupModel>())
            .Where(m => m.LeagueId is not null && m.Week is not null)
            .GroupBy(m => new { m.LeagueId, m.Week });

        foreach (var group in groupedByWeek)
        {
            if (!playoffStartByLeagueId.TryGetValue(group.Key.LeagueId!, out var playoffWeekStart))
            {
                continue;
            }

            if (!int.TryParse(group.Key.Week, out var week) || week >= playoffWeekStart)
            {
                continue;
            }

            var minPoints = group.Min(m => m.Points);
            foreach (var loser in group.Where(m => m.Points == minPoints))
            {
                if (lowScoringWeeksByRosterId.TryGetValue(loser.RosterId, out var count))
                {
                    lowScoringWeeksByRosterId[loser.RosterId] = count + 1;
                }
                else
                {
                    lowScoringWeeksByRosterId[loser.RosterId] = 1;
                }
            }
        }

        return lowScoringWeeksByRosterId;
    }
}