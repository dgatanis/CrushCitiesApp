using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SleeperFunctions.Application;

namespace SleeperFunctions.TransactionFunctions;

public class TransactionFunctions(ITransactionService transactionService)
{
    private readonly ITransactionService _transactionService = transactionService;

    [Function("GetTransactionsAsync")]
    public async Task<IActionResult> GetTransactionsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}/transactions/{week}")] HttpRequest req, string league_id, string week)
    {
        var result = await _transactionService.GetTransactionsAsync(league_id,week);
        return new OkObjectResult(result);
    }
}