using Shared.Models;

namespace SleeperFunctions.Application;

public interface IRosterService
{
    Task<List<RostersModel>> GetRostersAsync(string league_id);
}