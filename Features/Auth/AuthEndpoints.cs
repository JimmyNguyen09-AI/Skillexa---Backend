using skillexa_backend.Common.Results;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Infrastructure.Security;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;

namespace skillexa_backend.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/refresh", async (RefreshTokenRequest request, IAuthService service, CancellationToken cancellationToken) =>
        {
            var result = await service.RefreshAsync(request, cancellationToken);
            return Results.Ok(ApiResponse<AuthTokenResponse>.Ok(result, "Token refreshed."));
        }).AllowAnonymous();

        group.MapPost("/revoke", async (
            RevokeRefreshTokenRequest request,
            HttpContext httpContext,
            IAuthService service,
            CancellationToken cancellationToken) =>
        {
            await service.RevokeRefreshTokenAsync(httpContext.User.GetRequiredUserId(), request, cancellationToken);
            return Results.Ok(ApiResponse<string>.Ok("ok", "Refresh token revoked."));
        }).RequireAuthorization();

        group.MapGet("/google/login", (HttpContext httpContext, IConfiguration configuration) =>
            CreateOAuthChallenge(httpContext, "Google", configuration)).AllowAnonymous();

        group.MapGet("/oauth/callback", async (
            HttpContext httpContext,
            IConfiguration configuration,
            IAuthService service,
            CancellationToken cancellationToken) =>
        {
            var externalAuth = await httpContext.AuthenticateAsync("ExternalCookie");
            if (!externalAuth.Succeeded || externalAuth.Principal is null)
            {
                return Results.Redirect(BuildFrontendErrorRedirect(httpContext, configuration, "OAuth authentication failed."));
            }

            var email = externalAuth.Principal.FindFirstValue(ClaimTypes.Email)
                ?? externalAuth.Principal.FindFirstValue("email");
            var name = externalAuth.Principal.FindFirstValue(ClaimTypes.Name)
                ?? externalAuth.Principal.FindFirstValue("name")
                ?? "Skillexa User";
            var avatarUrl = externalAuth.Principal.FindFirstValue("picture");

            try
            {
                var tokens = await service.ExternalLoginAsync(email ?? string.Empty, name, avatarUrl, cancellationToken);
                await httpContext.SignOutAsync("ExternalCookie");
                WriteTemporaryOAuthSessionCookie(httpContext, tokens);
                return Results.Redirect(BuildFrontendSuccessRedirect(httpContext, configuration, tokens));
            }
            catch (Exception ex) when (ex is AppException or InvalidOperationException)
            {
                await httpContext.SignOutAsync("ExternalCookie");
                return Results.Redirect(BuildFrontendErrorRedirect(httpContext, configuration, ex.Message));
            }
        }).AllowAnonymous();

        return app;
    }

    private static IResult CreateOAuthChallenge(HttpContext httpContext, string providerScheme, IConfiguration configuration)
    {
        var frontendUrl = ResolveFrontendCallbackUrl(httpContext, configuration);
        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            throw new InvalidOperationException("AuthFrontend:CallbackUrl is required for OAuth login.");
        }

        var redirectUri = QueryHelpers.AddQueryString("/api/auth/oauth/callback", new Dictionary<string, string?>()
        {
            ["returnUrl"] = frontendUrl
        });

        var props = new AuthenticationProperties
        {
            RedirectUri = redirectUri
        };

        return Results.Challenge(props, [providerScheme]);
    }

    private static string BuildFrontendSuccessRedirect(HttpContext httpContext, IConfiguration configuration, AuthTokenResponse tokens)
    {
        var callbackUrl = ResolveFrontendCallbackUrl(httpContext, configuration);
        var query = new Dictionary<string, string?>
        {
            ["accessToken"] = tokens.AccessToken,
            ["refreshToken"] = tokens.RefreshToken,
            ["accessTokenExpiresAtUtc"] = tokens.AccessTokenExpiresAtUtc.ToString("O"),
            ["refreshTokenExpiresAtUtc"] = tokens.RefreshTokenExpiresAtUtc.ToString("O"),
            ["userId"] = tokens.UserId.ToString(),
            ["email"] = tokens.Email,
            ["name"] = tokens.Name,
            ["role"] = tokens.Role
        };

        return QueryHelpers.AddQueryString(callbackUrl, query);
    }

    private static string BuildFrontendErrorRedirect(HttpContext httpContext, IConfiguration configuration, string message)
    {
        var callbackUrl = ResolveFrontendCallbackUrl(httpContext, configuration);
        return QueryHelpers.AddQueryString(callbackUrl, new Dictionary<string, string?> { ["error"] = message });
    }

    private static string ResolveFrontendCallbackUrl(HttpContext httpContext, IConfiguration configuration)
    {
        var returnUrl = httpContext.Request.Query["returnUrl"].ToString();
        if (!string.IsNullOrWhiteSpace(returnUrl) && Uri.TryCreate(returnUrl, UriKind.Absolute, out _))
        {
            return returnUrl;
        }

        return configuration["AuthFrontend:CallbackUrl"]
            ?? throw new InvalidOperationException("AuthFrontend:CallbackUrl is required.");
    }

    private static void WriteTemporaryOAuthSessionCookie(HttpContext httpContext, AuthTokenResponse tokens)
    {
        var payload = JsonSerializer.Serialize(tokens);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(payload));

        httpContext.Response.Cookies.Append("skillexa_oauth_session", encoded, new CookieOptions
        {
            HttpOnly = false,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = false,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddMinutes(2)
        });
    }
}
