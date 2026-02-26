using System.Text.Json.Serialization;

namespace Shared.Models;

public sealed class RostersModel
{
    [JsonPropertyName("starters")]
    public List<string>? Starters { get; set; }

    [JsonPropertyName("settings")]
    public RosterSettingsModel? Settings { get; set; }

    [JsonPropertyName("roster_id")]
    public int RosterId { get; set; }

    [JsonPropertyName("reserve")]
    public List<string>? Reserve { get; set; }

    [JsonPropertyName("players")]
    public List<string>? Players { get; set; }

    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }

    [JsonPropertyName("league_id")]
    public string? LeagueId { get; set; }

    [JsonPropertyName("taxi")]
    public List<string>? Taxi { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

}

public sealed class RosterSettingsModel
{
    [JsonPropertyName("wins")]
    public int Wins { get; set; }

    [JsonPropertyName("waiver_position")]
    public int WaiverPosition { get; set; }

    [JsonPropertyName("waiver_budget_used")]
    public int WaiverBudgetUsed { get; set; }

    [JsonPropertyName("total_moves")]
    public int TotalMoves { get; set; }

    [JsonPropertyName("ties")]
    public int Ties { get; set; }

    [JsonPropertyName("losses")]
    public int Losses { get; set; }

    [JsonPropertyName("fpts_decimal")]
    public int FptsDecimal { get; set; }

    [JsonPropertyName("fpts_against_decimal")]
    public int FptsAgainstDecimal { get; set; }

    [JsonPropertyName("fpts_against")]
    public int FptsAgainst { get; set; }

    [JsonPropertyName("fpts")]
    public int Fpts { get; set; }
}
