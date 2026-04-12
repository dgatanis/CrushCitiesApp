using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SleeperFunctions.Application;

namespace SleeperFunctions.RosterFunctions;

public class RosterFunctions(IRosterService rosterService)
{
    private readonly IRosterService _rosterService = rosterService;
    
    [Function("GetRostersAsync")]
    public async Task<IActionResult> GetRostersAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}/rosters")] HttpRequest req, string league_id)
    {
        var result = await _rosterService.GetRostersAsync(league_id);
        return new OkObjectResult(result);
    }
}