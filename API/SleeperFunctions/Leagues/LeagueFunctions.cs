using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SleeperFunctions.Application;

namespace SleeperFunctions.LeagueFunctions;

public class LeagueFunctions(ILeagueService leagueService)
{
    private readonly ILeagueService _leagueService = leagueService;


    [Function("GetLeagueByIdAsync")]
    public async Task<IActionResult> GetLeagueByIdAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}")] HttpRequest req, string league_id)
    {
        var result = await _leagueService.GetLeagueByIdAsync(league_id);
        return new OkObjectResult(result);
    }



    [Function("GetLeagueBySeasonAsync")]
    public async Task<IActionResult> GetLeagueBySeasonAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/leagues/nfl/{season}")] HttpRequest req, string season)
    {
        var result = await _leagueService.GetLeagueBySeasonAsync(season);
        return new OkObjectResult(result);
    }


    [Function("GetNFLStateAsync")]
    public async Task<IActionResult> GetNFLStateAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "state/nfl")] HttpRequest req)
    {
        var result = await _leagueService.GetNFLStateAsync();
        return new OkObjectResult(result);
    }

}