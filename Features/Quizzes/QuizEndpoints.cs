using skillexa_backend.Common.Results;
using skillexa_backend.Infrastructure.Security;

namespace skillexa_backend.Features.Quizzes;

public static class QuizEndpoints
{
    public static IEndpointRouteBuilder MapQuizEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Quizzes");

        group.MapGet("/lessons/{lessonId:guid}/quiz", async (Guid lessonId, HttpContext httpContext, IQuizService service, CancellationToken cancellationToken) =>
        {
            var includeAnswers = httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsAdmin();
            var result = await service.GetByLessonAsync(lessonId, includeAnswers, cancellationToken);
            return Results.Ok(ApiResponse<QuizDto>.Ok(result));
        }).AllowAnonymous();

        group.MapPut("/lessons/{lessonId:guid}/quiz", async (Guid lessonId, UpsertQuizRequest request, IQuizService service, CancellationToken cancellationToken) =>
        {
            var result = await service.UpsertAsync(lessonId, request, cancellationToken);
            return Results.Ok(ApiResponse<QuizDto>.Ok(result, "Quiz saved."));
        }).RequireAuthorization("AdminOnly");

        group.MapDelete("/lessons/{lessonId:guid}/quiz", async (Guid lessonId, IQuizService service, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(lessonId, cancellationToken);
            return Results.Ok(ApiResponse<string>.Ok("ok", "Quiz deleted."));
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/lessons/{lessonId:guid}/quiz/submit", async (Guid lessonId, SubmitQuizRequest request, HttpContext httpContext, IQuizService service, CancellationToken cancellationToken) =>
        {
            var result = await service.SubmitAsync(httpContext.User.GetRequiredUserId(), lessonId, request, cancellationToken);
            return Results.Ok(ApiResponse<QuizResultDto>.Ok(result, "Quiz submitted."));
        }).RequireAuthorization();

        return app;
    }
}
