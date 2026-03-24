using skillexa_backend.Common.Results;
using skillexa_backend.Infrastructure.Security;

namespace skillexa_backend.Features.Lessons;

public static class LessonEndpoints
{
    public static IEndpointRouteBuilder MapLessonEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Lessons");

        group.MapGet("/courses/{courseId:guid}/lessons", async (Guid courseId, HttpContext httpContext, ILessonService service, CancellationToken cancellationToken) =>
        {
            var includeUnpublished = httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsAdmin();
            var result = await service.GetLessonsByCourseAsync(courseId, includeUnpublished, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyList<LessonDto>>.Ok(result));
        }).AllowAnonymous();

        group.MapGet("/lessons/{lessonId:guid}", async (Guid lessonId, HttpContext httpContext, ILessonService service, CancellationToken cancellationToken) =>
        {
            var includeUnpublished = httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsAdmin();
            var result = await service.GetLessonByIdAsync(lessonId, includeUnpublished, cancellationToken);
            return Results.Ok(ApiResponse<LessonDto>.Ok(result));
        }).AllowAnonymous();

        group.MapPost("/courses/{courseId:guid}/lessons", async (Guid courseId, UpsertLessonRequest request, ILessonService service, CancellationToken cancellationToken) =>
        {
            var result = await service.UpsertLessonAsync(courseId, null, request, cancellationToken);
            return Results.Created($"/api/lessons/{result.Id}", ApiResponse<LessonDto>.Ok(result, "Lesson created."));
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/courses/{courseId:guid}/lessons/{lessonId:guid}", async (Guid courseId, Guid lessonId, UpsertLessonRequest request, ILessonService service, CancellationToken cancellationToken) =>
        {
            var result = await service.UpsertLessonAsync(courseId, lessonId, request, cancellationToken);
            return Results.Ok(ApiResponse<LessonDto>.Ok(result, "Lesson updated."));
        }).RequireAuthorization("AdminOnly");

        group.MapDelete("/courses/{courseId:guid}/lessons/{lessonId:guid}", async (Guid courseId, Guid lessonId, ILessonService service, CancellationToken cancellationToken) =>
        {
            await service.DeleteLessonAsync(courseId, lessonId, cancellationToken);
            return Results.Ok(ApiResponse<string>.Ok("ok", "Lesson deleted."));
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/courses/{courseId:guid}/lesson-progress", async (Guid courseId, HttpContext httpContext, ILessonService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetCourseProgressAsync(httpContext.User.GetRequiredUserId(), courseId, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyList<CourseLessonProgressDto>>.Ok(result));
        }).RequireAuthorization();

        group.MapPut("/lessons/{lessonId:guid}/progress", async (Guid lessonId, LessonProgressRequest request, HttpContext httpContext, ILessonService service, CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateProgressAsync(httpContext.User.GetRequiredUserId(), lessonId, request, cancellationToken);
            return Results.Ok(ApiResponse<LessonProgressDto>.Ok(result, "Lesson progress updated."));
        }).RequireAuthorization();

        return app;
    }
}
