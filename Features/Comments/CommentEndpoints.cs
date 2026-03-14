using skillexa_backend.Common.Results;
using skillexa_backend.Infrastructure.Security;

namespace skillexa_backend.Features.Comments;

public static class CommentEndpoints
{
    public static IEndpointRouteBuilder MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Comments");

        group.MapGet("/lessons/{lessonId:guid}/comments", async (Guid lessonId, ICommentService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetLessonCommentsAsync(lessonId, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyList<CommentDto>>.Ok(result));
        }).AllowAnonymous();

        group.MapPost("/lessons/{lessonId:guid}/comments", async (Guid lessonId, CreateCommentRequest request, HttpContext httpContext, ICommentService service, CancellationToken cancellationToken) =>
        {
            var result = await service.CreateAsync(httpContext.User.GetRequiredUserId(), lessonId, request, cancellationToken);
            return Results.Ok(ApiResponse<CommentDto>.Ok(result, "Comment created."));
        }).RequireAuthorization();

        group.MapPut("/comments/{commentId:guid}", async (Guid commentId, UpdateCommentRequest request, HttpContext httpContext, ICommentService service, CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateAsync(httpContext.User.GetRequiredUserId(), httpContext.User.IsAdmin(), commentId, request, cancellationToken);
            return Results.Ok(ApiResponse<CommentDto>.Ok(result, "Comment updated."));
        }).RequireAuthorization();

        group.MapDelete("/comments/{commentId:guid}", async (Guid commentId, HttpContext httpContext, ICommentService service, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(httpContext.User.GetRequiredUserId(), httpContext.User.IsAdmin(), commentId, cancellationToken);
            return Results.Ok(ApiResponse<string>.Ok("ok", "Comment deleted."));
        }).RequireAuthorization();

        return app;
    }
}
