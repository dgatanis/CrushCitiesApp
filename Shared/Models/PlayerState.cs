using Shared.Models;

namespace Shared.Services;

public sealed class PlayerState
{
    public Dictionary<string, PlayerLiteModel>? Players { get; private set; }
    public bool IsLoaded => Players is not null;

    public void SetPlayers(Dictionary<string, PlayerLiteModel> players) => Players = players;

    public PlayerLiteModel? GetById(string playerId) =>
        Players is not null && Players.TryGetValue(playerId, out var p) ? p : null;
}
