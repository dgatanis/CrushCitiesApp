using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SleeperFunctions.Application;

namespace SleeperFunctions.Functions;
public class PlayerFunctions(IPlayerService playerService)
{
    private readonly IPlayerService _playerService = playerService;


    [Function("GetPlayersAsync")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "players/nfl")] HttpRequest req)
    {
        var result = await _playerService.GetPlayersAsync();
        return new OkObjectResult(result);
    }
}