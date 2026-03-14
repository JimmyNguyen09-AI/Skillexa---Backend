namespace skillexa_backend.Features.Auth;

public interface IAuthService
{
    Task<AuthTokenResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task RevokeRefreshTokenAsync(Guid userId, RevokeRefreshTokenRequest request, CancellationToken cancellationToken);
    Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken);
}
