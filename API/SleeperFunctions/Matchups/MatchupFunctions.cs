using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SleeperFunctions.Application;

namespace SleeperFunctions.MatchupFunctions;

public class MatchupFunctions(IMatchupService matchupService)
{

    private readonly IMatchupService _matchupService = matchupService;

    [Function("GetMatchupsAsync")]
    public async Task<IActionResult> GetMatchupsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}/matchups/{week}")] HttpRequest req, string league_id, string week)
    {
        var result = await _matchupService.GetMatchupsAsync(league_id, week);
        return new OkObjectResult(result);
    }


    [Function("GetPlayoffWinnersAsync")]
    public async Task<IActionResult> GetPlayoffWinnersAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}/winners_bracket")] HttpRequest req, string league_id)
    {
        var result = await _matchupService.GetPlayoffWinnersAsync(league_id);
        return new OkObjectResult(result);
    }


    [Function("GetPlayoffLosersAsync")]
    public async Task<IActionResult> GetPlayoffLosersAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}/losers_bracket")] HttpRequest req, string league_id)
    {
        var result = await _matchupService.GetPlayoffLosersAsync(league_id);
        return new OkObjectResult(result);
    }
}