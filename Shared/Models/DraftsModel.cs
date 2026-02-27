using System.Text.Json.Serialization;
using System.Text.Json;

namespace Shared.Models;

public sealed class DraftsModel
{
    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("creators")]
    public List<string>? Creators { get; set; }

    [JsonPropertyName("draft_id")]
    public string? DraftId { get; set; }

    [JsonPropertyName("draft_order")]
    public Dictionary<string, int>? DraftOrder { get; set; }

    [JsonPropertyName("last_message_id")]
    public string? LastMessageId { get; set; }

    [JsonPropertyName("last_message_time")]
    public long? LastMessageTime { get; set; }

    [JsonPropertyName("last_picked")]
    public JsonElement? LastPicked { get; set; }

    [JsonPropertyName("league_id")]
    public string? LeagueId { get; set; }

    [JsonPropertyName("metadata")]
    public DraftMetadataModel? Metadata { get; set; }

    [JsonPropertyName("season")]
    public string? Season { get; set; }

    [JsonPropertyName("season_type")]
    public string? SeasonType { get; set; }

    [JsonPropertyName("settings")]
    public DraftSettingsModel? Settings { get; set; }

    [JsonPropertyName("sport")]
    public string? Sport { get; set; }

    [JsonPropertyName("start_time")]
    public long? StartTime { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public sealed class DraftMetadataModel
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("scoring_type")]
    public string? ScoringType { get; set; }

    [JsonPropertyName("show_team_names")]
    public string? ShowTeamNames { get; set; }
}

public sealed class DraftSettingsModel
{
    [JsonPropertyName("alpha_sort")]
    public int AlphaSort { get; set; }

    [JsonPropertyName("autopause_enabled")]
    public int AutopauseEnabled { get; set; }

    [JsonPropertyName("autopause_end_time")]
    public int AutopauseEndTime { get; set; }

    [JsonPropertyName("autopause_start_time")]
    public int AutopauseStartTime { get; set; }

    [JsonPropertyName("autostart")]
    public int Autostart { get; set; }

    [JsonPropertyName("cpu_autopick")]
    public int CpuAutopick { get; set; }

    [JsonPropertyName("enforce_position_limits")]
    public int EnforcePositionLimits { get; set; }

    [JsonPropertyName("nomination_timer")]
    public int NominationTimer { get; set; }

    [JsonPropertyName("pick_timer")]
    public int PickTimer { get; set; }

    [JsonPropertyName("player_type")]
    public int PlayerType { get; set; }

    [JsonPropertyName("reversal_round")]
    public int ReversalRound { get; set; }

    [JsonPropertyName("rounds")]
    public int Rounds { get; set; }

    [JsonPropertyName("slots_bn")]
    public int SlotsBn { get; set; }

    [JsonPropertyName("slots_flex")]
    public int SlotsFlex { get; set; }

    [JsonPropertyName("slots_qb")]
    public int SlotsQb { get; set; }

    [JsonPropertyName("slots_rb")]
    public int SlotsRb { get; set; }

    [JsonPropertyName("slots_super_flex")]
    public int SlotsSuperFlex { get; set; }

    [JsonPropertyName("slots_te")]
    public int SlotsTe { get; set; }

    [JsonPropertyName("slots_wr")]
    public int SlotsWr { get; set; }

    [JsonPropertyName("teams")]
    public int Teams { get; set; }
}
