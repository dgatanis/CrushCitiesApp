using Shared.Models;

namespace SleeperFunctions.Application;

public interface ITransactionService
{
    Task<List<TransactionsModel>> GetTransactionsAsync(string league_id, string week);
}
