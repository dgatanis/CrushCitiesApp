using System.Text.Json.Serialization;

namespace Shared.Models;

public sealed class MiniMatchupModel
{
    public double Points { get; set; }
    public int RosterId { get; set; }
    public int? MatchupId { get; set; }
    public List<string>? Starters { get; set; }
    public Dictionary<string, double>? PlayersPoints { get; set; }
    public string? Season { get; set; }
    public string? Week { get; set; }
    public string? LeagueId { get; set; }
}
