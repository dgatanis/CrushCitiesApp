using System.Text.Json;
using Shared.Models;

namespace Shared.Services;

public interface ISleeperAPI
{
    Task<LeagueModel> GetLeagueAsync(string league_id);
    Task<Dictionary<string, PlayersModel>?> GetNFLPlayerDataAsync();
    Task<NFLStateModel?> GetNFLState();
    Task<List<LeagueModel>> GetLeagueBySeasonAsync(string season);
    Task<List<RostersModel>> GetRostersForLeagueAsync(string league_id);
    Task<List<UsersModel>> GetUsersForLeagueAsync(string league_id);
    Task<List<DraftsModel>> GetDraftsForLeagueAsync(string league_id);
    Task<List<DraftPicksModel>> GetDraftPicksForDraftAsync(string draft_id);
    Task<List<MatchupModel>> GetMatchupsForWeekAsync(string league_id, string week);
    Task<List<TransactionsModel>> GetTransactionsForWeekAsync(string league_id, string week);
    Task<List<PlayoffBracketsModel>> GetPlayoffWinnersBracketAsync(string league_id);
    Task<List<PlayoffBracketsModel>> GetPlayoffLosersBracketAsync(string league_id);
}