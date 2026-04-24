using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Models;

public sealed class UsersModel
{
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("is_bot")]
    public bool IsBot { get; set; }

    [JsonPropertyName("is_owner")]
    public bool IsOwner { get; set; }

    [JsonPropertyName("league_id")]
    public string? LeagueId { get; set; }

    [JsonPropertyName("metadata")]
    public UserMetadataModel? Metadata { get; set; }

    [JsonPropertyName("settings")]
    public JsonElement? Settings { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}

public sealed class UserMetadataModel
{
    [JsonPropertyName("allow_pn")]
    public string? AllowPn { get; set; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("mention_pn")]
    public string? MentionPn { get; set; }

    [JsonPropertyName("team_name")]
    public string? TeamName { get; set; }
}
