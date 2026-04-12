using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SleeperFunctions.Application;

namespace SleeperFunctions.UserFunctions;

public class UserFunctions(IUserService userService)
{
    private readonly IUserService _userService = userService;

    [Function("GetUsersAsync")]
    public async Task<IActionResult> GetUsersAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}/users")] HttpRequest req, string league_id)
    {
        var result = await _userService.GetUsersAsync(league_id);
        return new OkObjectResult(result);
    }
}