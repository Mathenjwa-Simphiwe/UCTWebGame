namespace UCTWebGame.DTOs;

public sealed record AuthUser(string Id, string Username, string Email);

public sealed record AuthResponse(string AccessToken, AuthUser User);
