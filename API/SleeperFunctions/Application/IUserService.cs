using Shared.Models;

namespace SleeperFunctions.Application;

public interface IUserService
{
    Task<List<UsersModel>> GetUsersAsync(string league_id);
}