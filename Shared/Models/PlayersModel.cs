using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Models;

// API response shape is a dictionary keyed by player_id, e.g. "6744": { ...player fields... }
// This is the full player model from the Sleeper API, but we use a lighter version to improve performance
public sealed class PlayersModel
{
    [JsonPropertyName("swish_id")]
    public int? SwishId { get; set; }

    [JsonPropertyName("injury_notes")]
    public string? InjuryNotes { get; set; }

    [JsonPropertyName("number")]
    public int? Number { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    [JsonPropertyName("fantasy_positions")]
    public List<string>? FantasyPositions { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("fantasy_data_id")]
    public int? FantasyDataId { get; set; }

    [JsonPropertyName("stats_id")]
    public int? StatsId { get; set; }

    [JsonPropertyName("depth_chart_order")]
    public int? DepthChartOrder { get; set; }

    [JsonPropertyName("birth_state")]
    public string? BirthState { get; set; }

    [JsonPropertyName("active")]
    public bool? Active { get; set; }

    [JsonPropertyName("oddsjam_id")]
    public string? OddsjamId { get; set; }

    [JsonPropertyName("high_school")]
    public string? HighSchool { get; set; }

    [JsonPropertyName("depth_chart_position")]
    public string? DepthChartPosition { get; set; }

    [JsonPropertyName("practice_description")]
    public string? PracticeDescription { get; set; }

    [JsonPropertyName("search_full_name")]
    public string? SearchFullName { get; set; }

    [JsonPropertyName("years_exp")]
    public int? YearsExp { get; set; }

    [JsonPropertyName("height")]
    public string? Height { get; set; }

    [JsonPropertyName("news_updated")]
    public long? NewsUpdated { get; set; }

    [JsonPropertyName("team_changed_at")]
    public long? TeamChangedAt { get; set; }

    [JsonPropertyName("rotoworld_id")]
    public int? RotoworldId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("player_shard")]
    public string? PlayerShard { get; set; }

    [JsonPropertyName("espn_id")]
    public int? EspnId { get; set; }

    [JsonPropertyName("birth_date")]
    public string? BirthDate { get; set; }

    [JsonPropertyName("injury_start_date")]
    public string? InjuryStartDate { get; set; }

    [JsonPropertyName("yahoo_id")]
    public int? YahooId { get; set; }

    [JsonPropertyName("team")]
    public string? Team { get; set; }

    [JsonPropertyName("pandascore_id")]
    public int? PandascoreId { get; set; }

    [JsonPropertyName("gsis_id")]
    public string? GsisId { get; set; }

    [JsonPropertyName("birth_country")]
    public string? BirthCountry { get; set; }

    [JsonPropertyName("player_id")]
    public string? PlayerId { get; set; }

    [JsonPropertyName("search_last_name")]
    public string? SearchLastName { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("weight")]
    public string? Weight { get; set; }

    [JsonPropertyName("sport")]
    public string? Sport { get; set; }

    [JsonPropertyName("injury_status")]
    public string? InjuryStatus { get; set; }

    [JsonPropertyName("rotowire_id")]
    public int? RotowireId { get; set; }

    [JsonPropertyName("practice_participation")]
    public string? PracticeParticipation { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("opta_id")]
    public string? OptaId { get; set; }

    [JsonPropertyName("sportradar_id")]
    public string? SportradarId { get; set; }

    [JsonPropertyName("injury_body_part")]
    public string? InjuryBodyPart { get; set; }

    [JsonPropertyName("kalshi_id")]
    public string? KalshiId { get; set; }

    [JsonPropertyName("search_rank")]
    public int? SearchRank { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("competitions")]
    public List<JsonElement>? Competitions { get; set; }

    [JsonPropertyName("age")]
    public int? Age { get; set; }

    [JsonPropertyName("hashtag")]
    public string? Hashtag { get; set; }

    [JsonPropertyName("college")]
    public string? College { get; set; }

    [JsonPropertyName("search_first_name")]
    public string? SearchFirstName { get; set; }

    [JsonPropertyName("birth_city")]
    public string? BirthCity { get; set; }
}
