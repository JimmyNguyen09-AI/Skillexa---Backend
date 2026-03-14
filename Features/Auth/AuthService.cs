using System.Net;
using Microsoft.EntityFrameworkCore;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Domain.Entities;
using skillexa_backend.Domain.Enums;
using skillexa_backend.Infrastructure.Data;
using skillexa_backend.Infrastructure.Security;

namespace skillexa_backend.Features.Auth;

public sealed class AuthService(AppDbContext dbContext, IJwtService jwtService, IConfiguration configuration) : IAuthService
{
    private readonly int _accessTokenMinutes = configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 60;

    public async Task<AuthTokenResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AppException("Name, email, and password are required.");
        }

        var exists = await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);
        if (exists)
        {
            throw new AppException("Email is already registered.", HttpStatusCode.Conflict);
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim(),
            Role = UserRole.User,
            Status = UserStatus.Active
        };

        dbContext.Users.Add(user);
        var response = CreateAuthResponse(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = response.RefreshToken,
            ExpiresAtUtc = response.RefreshTokenExpiresAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw new AppException("Invalid email or password.", HttpStatusCode.Unauthorized);

        if (user.Status != UserStatus.Active)
        {
            throw new AppException("User account is inactive.", HttpStatusCode.Forbidden);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new AppException("Invalid email or password.", HttpStatusCode.Unauthorized);
        }

        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);

        var response = CreateAuthResponse(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = response.RefreshToken,
            ExpiresAtUtc = response.RefreshTokenExpiresAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<AuthTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var refreshToken = await dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken)
            ?? throw new AppException("Refresh token is invalid.", HttpStatusCode.Unauthorized);

        if (refreshToken.RevokedAtUtc is not null || refreshToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new AppException("Refresh token has expired or was revoked.", HttpStatusCode.Unauthorized);
        }

        if (refreshToken.User.Status != UserStatus.Active)
        {
            throw new AppException("User account is inactive.", HttpStatusCode.Forbidden);
        }

        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        var response = CreateAuthResponse(refreshToken.User);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = refreshToken.UserId,
            Token = response.RefreshToken,
            ExpiresAtUtc = response.RefreshTokenExpiresAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task RevokeRefreshTokenAsync(Guid userId, RevokeRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await RevokeAllRefreshTokensAsync(userId, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var token = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken && x.UserId == userId, cancellationToken)
            ?? throw new AppException("Refresh token was not found.", HttpStatusCode.NotFound);

        token.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
        }
    }

    private AuthTokenResponse CreateAuthResponse(User user)
    {
        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();
        var accessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(_accessTokenMinutes);
        var refreshTokenExpiresAtUtc = jwtService.GetRefreshTokenExpiryUtc();

        return new AuthTokenResponse(
            accessToken,
            refreshToken,
            accessTokenExpiresAtUtc,
            refreshTokenExpiresAtUtc,
            user.Id,
            user.Email,
            user.Name,
            user.Role.ToString());
    }
}
