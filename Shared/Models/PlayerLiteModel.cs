using System.Text.Json.Serialization;

namespace Shared.Models;

public sealed class PlayerLiteModel
{
    [JsonPropertyName("player_id")]
    public string? PlayerId { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("firstname")]
    public string? Firstname { get; set; }

    [JsonPropertyName("lastname")]
    public string? Lastname { get; set; }

    [JsonPropertyName("age")]
    public int Age { get; set; }

    [JsonPropertyName("team")]
    public string? Team { get; set; }

    [JsonPropertyName("number")]
    public int? Number { get; set; }

    [JsonPropertyName("height")]
    public string? Height { get; set; }

    [JsonPropertyName("weight")]
    public string? Weight { get; set; }

    [JsonPropertyName("years_exp")]
    public int? YearsExp { get; set; }

    [JsonPropertyName("rotowire_id")]
    public int? RotowireId { get; set; }

    [JsonPropertyName("college")]
    public string? College { get; set; }

    [JsonPropertyName("search_rank")]
    public int? SearchRank { get; set; }

    [JsonPropertyName("injury_status")]
    public string? InjuryStatus { get; set; }

    [JsonPropertyName("injury_body_part")]
    public string? InjuryBodyPart { get; set; }
}
