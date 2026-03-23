using skillexa_backend.Common.Results;
using skillexa_backend.Infrastructure.Security;

namespace skillexa_backend.Features.Roadmaps;

public static class RoadmapEndpoints
{
    public static IEndpointRouteBuilder MapRoadmapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roadmaps").WithTags("Roadmaps").RequireAuthorization();

        group.MapGet("/", async (HttpContext httpContext, IRoadmapService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetRoadmapsAsync(
                httpContext.User.GetRequiredUserId(),
                httpContext.User.IsAdmin(),
                cancellationToken);

            return Results.Ok(ApiResponse<IReadOnlyList<RoadmapSummaryDto>>.Ok(result));
        });

        group.MapGet("/{slug}", async (string slug, HttpContext httpContext, IRoadmapService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetRoadmapBySlugAsync(
                httpContext.User.GetRequiredUserId(),
                slug,
                httpContext.User.IsAdmin(),
                cancellationToken);

            return Results.Ok(ApiResponse<RoadmapDetailDto>.Ok(result));
        });

        return app;
    }
}
