using System.Text.Json.Serialization;

namespace Shared.Models;

public sealed class TransactionsModel
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("metadata")]
    public TransactionMetadataModel? Metadata { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("settings")]
    public TransactionSettingsModel? Settings { get; set; }

    [JsonPropertyName("leg")]
    public int Leg { get; set; }

    [JsonPropertyName("draft_picks")]
    public List<TransactionDraftPickModel>? DraftPicks { get; set; }

    [JsonPropertyName("creator")]
    public string? Creator { get; set; }

    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    [JsonPropertyName("adds")]
    public Dictionary<string, int>? Adds { get; set; }

    [JsonPropertyName("drops")]
    public Dictionary<string, int>? Drops { get; set; }

    [JsonPropertyName("consenter_ids")]
    public List<int>? ConsenterIds { get; set; }

    [JsonPropertyName("roster_ids")]
    public List<int>? RosterIds { get; set; }

    [JsonPropertyName("status_updated")]
    public long StatusUpdated { get; set; }

    [JsonPropertyName("waiver_budget")]
    public List<TransactionWaiverBudgetModel>? WaiverBudget { get; set; }
}

public sealed class TransactionMetadataModel
{
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public sealed class TransactionSettingsModel
{
    [JsonPropertyName("seq")]
    public int Seq { get; set; }

    [JsonPropertyName("waiver_bid")]
    public int WaiverBid { get; set; }
}

public sealed class TransactionDraftPickModel
{
    [JsonPropertyName("round")]
    public int Round { get; set; }

    [JsonPropertyName("season")]
    public string? Season { get; set; }

    [JsonPropertyName("league_id")]
    public string? LeagueId { get; set; }

    [JsonPropertyName("roster_id")]
    public int RosterId { get; set; }

    [JsonPropertyName("owner_id")]
    public int OwnerId { get; set; }

    [JsonPropertyName("previous_owner_id")]
    public int PreviousOwnerId { get; set; }
}

public sealed class TransactionWaiverBudgetModel
{
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("receiver")]
    public int Receiver { get; set; }

    [JsonPropertyName("sender")]
    public int Sender { get; set; }
}
