using System.Net;
using System.Net.Http.Json;

namespace Client.Auth;

public class AuthService(
    HttpClient httpClient,
    TokenStore tokenStore,
    ApiAuthenticationStateProvider authenticationStateProvider) : IAuthService
{
    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/auth/login", new LoginRequest(username, password));
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (string.IsNullOrWhiteSpace(login?.Token))
            {
                return false;
            }

            await tokenStore.SetTokenAsync(login.Token);
            authenticationStateProvider.NotifyUserAuthentication(login.Token);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<(bool Succeeded, string Error)> RegisterAsync(string username, string email, string password)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/auth/register", new RegisterRequest(username, email, password));
            if (response.IsSuccessStatusCode)
            {
                return (true, string.Empty);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return (false, "Registration failed. Check username/email uniqueness and password requirements.");
            }

            return (false, "Registration failed.");
        }
        catch (HttpRequestException)
        {
            var apiUrl = httpClient.BaseAddress?.ToString() ?? "(not configured)";
            return (false, $"Cannot reach API at {apiUrl}. Verify the deployed API URL and CORS settings.");
        }
    }

    public async Task LogoutAsync()
    {
        await tokenStore.ClearTokenAsync();
        authenticationStateProvider.NotifyUserLogout();
    }
}
