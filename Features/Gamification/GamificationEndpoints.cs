using skillexa_backend.Common.Results;
using skillexa_backend.Infrastructure.Security;

namespace skillexa_backend.Features.Gamification;

public static class GamificationEndpoints
{
    public static IEndpointRouteBuilder MapGamificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gamification").WithTags("Gamification");

        group.MapGet("/me", async (HttpContext httpContext, IGamificationService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetProfileAsync(httpContext.User.GetRequiredUserId(), cancellationToken);
            return Results.Ok(ApiResponse<GamificationProfileDto>.Ok(result));
        }).RequireAuthorization();

        return app;
    }
}
