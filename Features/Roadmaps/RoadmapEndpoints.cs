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

        group.MapPost("/", async (UpsertRoadmapRequest request, HttpContext httpContext, IRoadmapService service, CancellationToken cancellationToken) =>
        {
            var result = await service.CreateAsync(
                httpContext.User.GetRequiredUserId(),
                request,
                httpContext.User.IsAdmin(),
                cancellationToken);

            return Results.Ok(ApiResponse<RoadmapDetailDto>.Ok(result, "Roadmap created."));
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/{roadmapId:guid}", async (Guid roadmapId, UpsertRoadmapRequest request, HttpContext httpContext, IRoadmapService service, CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateAsync(
                httpContext.User.GetRequiredUserId(),
                roadmapId,
                request,
                httpContext.User.IsAdmin(),
                cancellationToken);

            return Results.Ok(ApiResponse<RoadmapDetailDto>.Ok(result, "Roadmap updated."));
        }).RequireAuthorization("AdminOnly");

        group.MapDelete("/{roadmapId:guid}", async (Guid roadmapId, IRoadmapService service, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(roadmapId, cancellationToken);
            return Results.Ok(ApiResponse<bool>.Ok(true, "Roadmap removed."));
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/{roadmapId:guid}/courses", async (Guid roadmapId, UpsertRoadmapCourseRequest request, HttpContext httpContext, IRoadmapService service, CancellationToken cancellationToken) =>
        {
            var result = await service.AddCourseAsync(
                httpContext.User.GetRequiredUserId(),
                roadmapId,
                request,
                httpContext.User.IsAdmin(),
                cancellationToken);

            return Results.Ok(ApiResponse<RoadmapDetailDto>.Ok(result, "Roadmap updated."));
        }).RequireAuthorization("AdminOnly");

        group.MapDelete("/{roadmapId:guid}/courses/{courseId:guid}", async (Guid roadmapId, Guid courseId, HttpContext httpContext, IRoadmapService service, CancellationToken cancellationToken) =>
        {
            var result = await service.RemoveCourseAsync(
                httpContext.User.GetRequiredUserId(),
                roadmapId,
                courseId,
                httpContext.User.IsAdmin(),
                cancellationToken);

            return Results.Ok(ApiResponse<RoadmapDetailDto>.Ok(result, "Roadmap updated."));
        }).RequireAuthorization("AdminOnly");

        return app;
    }
}
