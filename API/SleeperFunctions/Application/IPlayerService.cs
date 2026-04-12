using Shared.Models;

namespace SleeperFunctions.Application;

public interface IPlayerService
{
    Task<Dictionary<string, PlayersModel>> GetPlayersAsync();
}