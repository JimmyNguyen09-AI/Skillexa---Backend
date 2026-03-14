namespace skillexa_backend.Features.Auth;

public sealed record RegisterRequest(string Name, string Email, string Password, string? AvatarUrl);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record RevokeRefreshTokenRequest(string RefreshToken);

public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc,
    Guid UserId,
    string Email,
    string Name,
    string Role);
