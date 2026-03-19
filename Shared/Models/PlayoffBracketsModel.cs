using System.Text.Json.Serialization;

public sealed class PlayoffBracketsModel
{
    [JsonPropertyName("p")]
    public int? PlacementGame { get; set; }

    [JsonPropertyName("m")]
    public int MatchId { get; set; }

    [JsonPropertyName("r")]
    public int Round { get; set; }

    [JsonPropertyName("l")]
    public int? LoserId { get; set; }

    [JsonPropertyName("w")]
    public int? WinnerId { get; set; }

    [JsonPropertyName("t1")]
    public int? Team1 { get; set; }

    [JsonPropertyName("t2")]
    public int? Team2 { get; set; }

    [JsonPropertyName("t1_from")]
    public FromBracket? T1From { get; set; }

    [JsonPropertyName("t2_from")]
    public FromBracket? T2From { get; set; }
}

public sealed class FromBracket
{
    [JsonPropertyName("w")]
    public int? WinnerMatchId { get; set; }

    [JsonPropertyName("l")]
    public int? LoserMatchId { get; set; }
}