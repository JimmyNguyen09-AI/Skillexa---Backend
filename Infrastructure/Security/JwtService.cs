using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using skillexa_backend.Domain.Entities;

namespace skillexa_backend.Infrastructure.Security;

public sealed class JwtService(IConfiguration configuration) : IJwtService
{
    private readonly string _secret = configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is required.");

    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "Skillexa";
    private readonly string _audience = configuration["Jwt:Audience"] ?? "Skillexa.Client";
    private readonly int _accessTokenMinutes = configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 60;
    private readonly int _refreshTokenDays = configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 7;

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public DateTime GetRefreshTokenExpiryUtc() => DateTime.UtcNow.AddDays(_refreshTokenDays);
}
