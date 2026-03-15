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

    public async Task<AuthTokenResponse> ExternalLoginAsync(string email, string name, string? avatarUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new AppException("OAuth provider did not return a valid email.", HttpStatusCode.BadRequest);
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Name = string.IsNullOrWhiteSpace(name) ? normalizedEmail.Split('@')[0] : name.Trim(),
                Email = normalizedEmail,
                PasswordHash = Guid.NewGuid().ToString("N"),
                AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim(),
                Role = UserRole.User,
                Status = UserStatus.Active
            };

            dbContext.Users.Add(user);
        }
        else
        {
            if (user.Status != UserStatus.Active)
            {
                throw new AppException("Tai khoan tam thoi bi vo hieu hoa. Vui long lien he quan tri vien.", HttpStatusCode.Forbidden);
            }

            user.Name = string.IsNullOrWhiteSpace(name) ? user.Name : name.Trim();
            user.AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? user.AvatarUrl : avatarUrl.Trim();
            user.UpdatedAtUtc = DateTime.UtcNow;
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
            throw new AppException("Tai khoan tam thoi bi vo hieu hoa. Vui long lien he quan tri vien.", HttpStatusCode.Forbidden);
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
