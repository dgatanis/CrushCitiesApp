using Shared.Models;

namespace Shared.Services;

public interface IPlayoffStats
{
    IReadOnlyDictionary<int, int> GetChampionshipCountByRosterId();
    IReadOnlyDictionary<int, int> GetFinalsAppearancesByRosterId();
    IReadOnlyDictionary<int, int> GetPlayoffAppearancesByRosterId();
}

public sealed class PlayoffStats(PlayoffData playoffData) : IPlayoffStats
{

    public IReadOnlyDictionary<int, int> GetChampionshipCountByRosterId()
    {
        Dictionary<int, int> championshipCountByRosterId = new ()
        {
            { 1, 0 },
            { 2, 1 },
            { 3, 0 },
            { 4, 1 },
            { 5, 0 },
            { 6, 0 },
            { 7, 1 },
            { 8, 0 },
            { 9, 0 },
            { 10, 1 }
        };

        foreach(var bracket in playoffData.AllWinnersBrackets)
        {
            foreach(var playoff in bracket.Value)
            {
                if(playoff.Round != 3 || playoff.PlacementGame != 1 || !playoff.WinnerId.HasValue || !playoff.LoserId.HasValue ) continue;
            
                var winnerId = playoff.WinnerId.Value;
                var loserId = playoff.LoserId.Value;

                championshipCountByRosterId[winnerId] = championshipCountByRosterId.TryGetValue(winnerId, out var champs)
                    ? champs + 1
                    : 1;
            }
        }
        return championshipCountByRosterId;
    }


    public IReadOnlyDictionary<int, int> GetFinalsAppearancesByRosterId()
    {

        Dictionary<int, int> finalsAppearancesByRosterId = new()
        {
            { 1, 0 },
            { 2, 1 },
            { 3, 2 }, 
            { 4, 1 },
            { 5, 0 },
            { 6, 0 },
            { 7, 1 },
            { 8, 0 },
            { 9, 0 },
            { 10, 3 }
        };
        
        foreach(var bracket in playoffData.AllWinnersBrackets)
        {
            foreach(var playoff in bracket.Value)
            {
                if(playoff.Round != 3 || playoff.PlacementGame != 1 || !playoff.WinnerId.HasValue || !playoff.LoserId.HasValue ) continue;
            
                var winnerId = playoff.WinnerId.Value;
                var loserId = playoff.LoserId.Value;

                // Finals appearance: both winner and loser made the finals
                finalsAppearancesByRosterId[winnerId] = finalsAppearancesByRosterId.TryGetValue(winnerId, out var wFinals)
                    ? wFinals + 1
                    : 1;

                finalsAppearancesByRosterId[loserId] = finalsAppearancesByRosterId.TryGetValue(loserId, out var lFinals)
                    ? lFinals + 1
                    : 1;
            }
        }
        return finalsAppearancesByRosterId;
    }

    
    public IReadOnlyDictionary<int, int> GetPlayoffAppearancesByRosterId()
    {
        Dictionary<int, int> playoffAppearancesByRosterId = new ()
        {
            { 1, 3 },
            { 2, 4 },
            { 3, 3 }, 
            { 4, 2 },
            { 5, 1 },
            { 6, 1 },
            { 7, 2 },
            { 8, 3 },
            { 9, 1 },
            { 10, 4 }
        };

        foreach (var bracket in playoffData.AllWinnersBrackets)
        {
            if (string.IsNullOrWhiteSpace(bracket.Key))
            {
                continue;
            }

            var rosterIdsThisSeason = new HashSet<int>();

            foreach (var playoff in bracket.Value)
            {
                if(!playoff.WinnerId.HasValue) continue;
                if (playoff.Team1.HasValue)
                {
                    rosterIdsThisSeason.Add(playoff.Team1.Value);
                }

                if (playoff.Team2.HasValue)
                {
                    rosterIdsThisSeason.Add(playoff.Team2.Value);
                }
            }

            // Count each roster once per season
            foreach (var rosterId in rosterIdsThisSeason)
            {
                playoffAppearancesByRosterId[rosterId] = playoffAppearancesByRosterId.TryGetValue(rosterId, out var count)
                    ? count + 1
                    : 1;
            }
        }

        return playoffAppearancesByRosterId;
    }
}