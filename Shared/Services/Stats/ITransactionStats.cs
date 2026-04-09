using Shared.Models;

namespace Shared.Services;

public interface ITransactionStats
{
    IReadOnlyDictionary<int, int> GetMostTradedWithRosterId();
    IReadOnlyDictionary<int, int> GetTradeCountsByRosterId();
}

public sealed class TransactionStats(TransactionData transactionData) : ITransactionStats
{

    public IReadOnlyDictionary<int, int> GetMostTradedWithRosterId()
    {
        Dictionary<int, int> mostTradedWithRosterId = new();
        var partnersByRoster = new Dictionary<int, Dictionary<int, int>>();
        var tradeCounts = new Dictionary<(int, int), int>();

        foreach (var transaction in transactionData.GetFilteredTransactionsData(["trade"]) ?? new List<TransactionsModel>())
        {
            if (transaction.ConsenterIds is null || transaction.ConsenterIds.Count < 2) continue;

            var consenters = transaction.ConsenterIds
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            for (var i = 0; i < consenters.Count - 1; i++)
            {
                for (var j = i + 1; j < consenters.Count; j++)
                {
                    var pair = (consenters[i], consenters[j]);
                    tradeCounts[pair] = tradeCounts.TryGetValue(pair, out var count) ? count + 1 : 1;
                }
            }
        }

        foreach (var kv in tradeCounts)
        {
            var a = kv.Key.Item1;
            var b = kv.Key.Item2;
            var count = kv.Value;

            if (!partnersByRoster.TryGetValue(a, out var aPartners))
                partnersByRoster[a] = aPartners = new Dictionary<int, int>();
            if (!partnersByRoster.TryGetValue(b, out var bPartners))
                partnersByRoster[b] = bPartners = new Dictionary<int, int>();

            aPartners[b] = count;
            bPartners[a] = count;
        }

        foreach (var roster in partnersByRoster)
        {
            var bestPartner = roster.Value
                .OrderByDescending(p => p.Value)
                .First().Key;

            mostTradedWithRosterId[roster.Key] = bestPartner;
        }

        return mostTradedWithRosterId;
    }


    public IReadOnlyDictionary<int, int> GetTradeCountsByRosterId()
    {
        Dictionary<int, int> tradeCountsByRosterId = new();
        var tradeCounts = new Dictionary<int, int>();

        foreach (var transaction in transactionData.GetFilteredTransactionsData(["trade"]) ?? new List<TransactionsModel>())
        {
            if (transaction.ConsenterIds is null) continue;

            foreach (var consenter in transaction.ConsenterIds)
            {
                tradeCounts[consenter] = tradeCounts.TryGetValue(consenter, out var count) ? count + 1 : 1;
            }
        }

        foreach (var kv in tradeCounts)
        {
            tradeCountsByRosterId[kv.Key] = kv.Value;
        }

        return tradeCountsByRosterId;
    }
}