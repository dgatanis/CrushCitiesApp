namespace Client.Auth;

public sealed record LoginRequest(string Username, string Password);
public sealed record RegisterRequest(string Username, string Email, string Password);
public sealed record LoginResponse(string Token);
