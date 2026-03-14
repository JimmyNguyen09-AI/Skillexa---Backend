using skillexa_backend.Domain.Entities;

namespace skillexa_backend.Infrastructure.Security;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    DateTime GetRefreshTokenExpiryUtc();
}
