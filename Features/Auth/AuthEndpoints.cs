using skillexa_backend.Common.Results;
using skillexa_backend.Infrastructure.Security;

namespace skillexa_backend.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, IAuthService service, CancellationToken cancellationToken) =>
        {
            var result = await service.RegisterAsync(request, cancellationToken);
            return Results.Ok(ApiResponse<AuthTokenResponse>.Ok(result, "Registration successful."));
        }).AllowAnonymous();

        group.MapPost("/login", async (LoginRequest request, IAuthService service, CancellationToken cancellationToken) =>
        {
            var result = await service.LoginAsync(request, cancellationToken);
            return Results.Ok(ApiResponse<AuthTokenResponse>.Ok(result, "Login successful."));
        }).AllowAnonymous();

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

        return app;
    }
}
