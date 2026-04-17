using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SleeperFunctions.Application;

namespace SleeperFunctions.Functions;

public class DraftFunctions(IDraftService draftService)
{
    private readonly IDraftService _draftService = draftService;
    

    [Function("GetDraftsAsync")]
    public async Task<IActionResult> GetDraftsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}/drafts")] HttpRequest req, string league_id)
    {
        var result = await _draftService.GetDraftsAsync(league_id);
        return new OkObjectResult(result);
    }


    [Function("GetDraftPicksAsync")]
    public async Task<IActionResult> GetDraftPicksAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "draft/{draft_id}/picks")] HttpRequest req, string draft_id)
    {
        var result = await _draftService.GetDraftPicksAsync(draft_id);
        return new OkObjectResult(result);
    }
}