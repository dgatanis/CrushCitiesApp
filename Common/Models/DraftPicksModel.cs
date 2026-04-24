using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Models;

public sealed class DraftPicksModel
{
    [JsonPropertyName("draft_id")]
    public string? DraftId { get; set; }

    [JsonPropertyName("draft_slot")]
    public int DraftSlot { get; set; }

    [JsonPropertyName("is_keeper")]
    public JsonElement? IsKeeper { get; set; }

    [JsonPropertyName("metadata")]
    public DraftPickMetadataModel? Metadata { get; set; }

    [JsonPropertyName("pick_no")]
    public int PickNo { get; set; }

    [JsonPropertyName("picked_by")]
    public string? PickedBy { get; set; }

    [JsonPropertyName("player_id")]
    public string? PlayerId { get; set; }

    [JsonPropertyName("reactions")]
    public JsonElement? Reactions { get; set; }

    [JsonPropertyName("roster_id")]
    public int RosterId { get; set; }

    [JsonPropertyName("round")]
    public int Round { get; set; }

    public string? OriginalPickOwner { get; set; }
}

public sealed class DraftPickMetadataModel
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("injury_status")]
    public string? InjuryStatus { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("news_updated")]
    public string? NewsUpdated { get; set; }

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("player_id")]
    public string? PlayerId { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("sport")]
    public string? Sport { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("team")]
    public string? Team { get; set; }

    [JsonPropertyName("team_abbr")]
    public string? TeamAbbr { get; set; }

    [JsonPropertyName("team_changed_at")]
    public string? TeamChangedAt { get; set; }

    [JsonPropertyName("years_exp")]
    public string? YearsExp { get; set; }
}
