using Shared.Models;

namespace Shared.Services;

public sealed class Normalizer : INormalizer
{
    public List<UsersModel> NormalizeUsers(List<UsersModel> users)
    {
        return users;
    }

    public List<RostersModel> NormalizeRosters(List<RostersModel> rosters)
    {
        return rosters;
    }
}