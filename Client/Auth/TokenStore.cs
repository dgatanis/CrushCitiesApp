using Microsoft.JSInterop;

namespace Client.Auth;

public class TokenStore(IJSRuntime jsRuntime)
{
    private const string TokenKey = "authToken";

    public async Task<string?> GetTokenAsync()
    {
        return await jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
    }

    public async Task SetTokenAsync(string token)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
    }

    public async Task ClearTokenAsync()
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
    }
}
