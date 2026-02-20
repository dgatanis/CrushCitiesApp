namespace Client.Auth;

public interface IAuthService
{
    Task<bool> LoginAsync(string username, string password);
    Task<(bool Succeeded, string Error)> RegisterAsync(string username, string email, string password);
    Task LogoutAsync();
}
