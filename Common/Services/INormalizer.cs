using Shared.Models;

namespace Shared.Services;

public interface INormalizer
{
    List<UsersModel> NormalizeUsers(List<UsersModel> users);
    List<RostersModel> NormalizeRosters(List<RostersModel> rosters);
}
