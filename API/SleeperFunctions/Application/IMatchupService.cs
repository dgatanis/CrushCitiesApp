using Shared.Models;

namespace SleeperFunctions.Application;

public interface IMatchupService
{
    Task<List<MatchupModel>> GetMatchupsAsync(string league_id, string week);
    Task<List<PlayoffBracketsModel>> GetPlayoffWinnersAsync(string league_id);
    Task<List<PlayoffBracketsModel>> GetPlayoffLosersAsync(string league_id);
}