namespace skillexa_backend.Features.Auth;

public interface IAuthService
{
    Task<AuthTokenResponse> ExternalLoginAsync(string email, string name, string? avatarUrl, CancellationToken cancellationToken);
    Task<AuthTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task RevokeRefreshTokenAsync(Guid userId, RevokeRefreshTokenRequest request, CancellationToken cancellationToken);
    Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken);
}
