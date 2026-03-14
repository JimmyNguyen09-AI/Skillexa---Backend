using skillexa_backend.Common.Results;
using skillexa_backend.Infrastructure.Security;

namespace skillexa_backend.Features.Courses;

public static class CourseEndpoints
{
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/courses").WithTags("Courses");

        group.MapGet("/", async (HttpContext httpContext, ICourseService service, CancellationToken cancellationToken) =>
        {
            var includeUnpublished = httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsAdmin();
            var result = await service.GetCoursesAsync(includeUnpublished, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyList<CourseSummaryDto>>.Ok(result));
        }).AllowAnonymous();

        group.MapGet("/{slug}", async (string slug, HttpContext httpContext, ICourseService service, CancellationToken cancellationToken) =>
        {
            var includeUnpublished = httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsAdmin();
            var result = await service.GetCourseBySlugAsync(slug, includeUnpublished, cancellationToken);
            return Results.Ok(ApiResponse<CourseDetailDto>.Ok(result));
        }).AllowAnonymous();

        group.MapPost("/", async (CreateCourseRequest request, ICourseService service, CancellationToken cancellationToken) =>
        {
            var result = await service.CreateCourseAsync(request, cancellationToken);
            return Results.Created($"/api/courses/{result.Slug}", ApiResponse<CourseDetailDto>.Ok(result, "Course created."));
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/{courseId:guid}", async (Guid courseId, UpdateCourseRequest request, ICourseService service, CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateCourseAsync(courseId, request, cancellationToken);
            return Results.Ok(ApiResponse<CourseDetailDto>.Ok(result, "Course updated."));
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/{courseId:guid}/enroll", async (Guid courseId, HttpContext httpContext, ICourseService service, CancellationToken cancellationToken) =>
        {
            var result = await service.EnrollAsync(httpContext.User.GetRequiredUserId(), courseId, cancellationToken);
            return Results.Ok(ApiResponse<EnrollmentDto>.Ok(result, "Enrollment created."));
        }).RequireAuthorization();

        return app;
    }
}
