using System.Text.Json.Serialization;

namespace Shared.Models;

public sealed class MatchupModel
{
    [JsonPropertyName("points")]
    public double Points { get; set; }

    [JsonPropertyName("players")]
    public List<string>? Players { get; set; }

    [JsonPropertyName("roster_id")]
    public int RosterId { get; set; }

    [JsonPropertyName("custom_points")]
    public double? CustomPoints { get; set; }

    [JsonPropertyName("matchup_id")]
    public int? MatchupId { get; set; }

    [JsonPropertyName("starters")]
    public List<string>? Starters { get; set; }

    [JsonPropertyName("starters_points")]
    public List<double>? StartersPoints { get; set; }

    [JsonPropertyName("players_points")]
    public Dictionary<string, double>? PlayersPoints { get; set; }

    public string? Season { get; set; }

    public string? Week { get; set; }

    public string? LeagueId { get; set; }
    
}
