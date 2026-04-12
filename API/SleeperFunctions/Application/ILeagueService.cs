using Shared.Models;

namespace SleeperFunctions.Application;

public interface ILeagueService
{
    Task<LeagueModel> GetLeagueByIdAsync(string league_id);
    Task<List<LeagueModel>> GetLeagueBySeasonAsync(string season);
    Task<NFLStateModel> GetNFLStateAsync();
}