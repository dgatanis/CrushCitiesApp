using System.Text.Json.Serialization;

namespace Shared.Models;

public sealed class NFLStateModel
{
    [JsonPropertyName("week")]
    public int Week { get; set; }

    [JsonPropertyName("leg")]
    public int Leg { get; set; }

    [JsonPropertyName("season")]
    public string? Season { get; set; }

    [JsonPropertyName("season_type")]
    public string? SeasonType { get; set; }

    [JsonPropertyName("league_season")]
    public string? LeagueSeason { get; set; }

    [JsonPropertyName("previous_season")]
    public string? PreviousSeason { get; set; }

    [JsonPropertyName("season_start_date")]
    public string? SeasonStartDate { get; set; }

    [JsonPropertyName("display_week")]
    public int DisplayWeek { get; set; }

    [JsonPropertyName("league_create_season")]
    public string? LeagueCreateSeason { get; set; }

    [JsonPropertyName("season_has_scores")]
    public bool SeasonHasScores { get; set; }
}