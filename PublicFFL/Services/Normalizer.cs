using Shared.Models;

namespace Shared.Services;

public sealed class Normalizer : INormalizer
{
    public List<UsersModel> NormalizeUsers(List<UsersModel> users)
    {
        foreach (var user in users)
        {
            var rng = new Random();
            user.Metadata!.TeamName = $"Team {rng.Next(1, 21)}";
                
        }
        return users;
    }

    public List<RostersModel> NormalizeRosters(List<RostersModel> rosters)
    {
        foreach (var roster in rosters)
        {
            if (roster.Metadata is not null)
            {
                var keysToRemove = roster.Metadata.Keys
                    .Where(k => k.StartsWith("p_nick_", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    roster.Metadata.Remove(key);
                }
            }
        }
        return rosters;
    }
}
