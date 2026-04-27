using skillexa_backend.Common.Results;
using skillexa_backend.Infrastructure.Security;

namespace skillexa_backend.Features.InterviewPractice;

public static class InterviewTopicEndpoints
{
    public static IEndpointRouteBuilder MapInterviewTopicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/interview-topics").WithTags("InterviewTopics");

        group.MapGet("/", async (HttpContext httpContext, IInterviewTopicService service, CancellationToken ct) =>
        {
            var result = await service.GetAllAsync(httpContext.User.GetRequiredUserId(), ct);
            return Results.Ok(ApiResponse<IReadOnlyList<InterviewTopicSummaryDto>>.Ok(result));
        }).RequireAuthorization();

        group.MapGet("/{slug}", async (string slug, HttpContext httpContext, IInterviewTopicService service, CancellationToken ct) =>
        {
            var result = await service.GetBySlugAsync(slug, httpContext.User.GetRequiredUserId(), ct);
            return Results.Ok(ApiResponse<InterviewTopicDetailDto>.Ok(result));
        }).RequireAuthorization();

        group.MapPost("/", async (CreateInterviewTopicRequest request, IInterviewTopicService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return Results.Created($"/api/interview-topics/{result.Slug}", ApiResponse<InterviewTopicDetailDto>.Ok(result, "Topic created."));
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/{id:guid}", async (Guid id, UpdateInterviewTopicRequest request, IInterviewTopicService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, request, ct);
            return Results.Ok(ApiResponse<InterviewTopicDetailDto>.Ok(result, "Topic updated."));
        }).RequireAuthorization("AdminOnly");

        group.MapDelete("/{id:guid}", async (Guid id, IInterviewTopicService service, CancellationToken ct) =>
        {
            await service.DeleteAsync(id, ct);
            return Results.Ok(ApiResponse<string>.Ok("ok", "Topic deleted."));
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/{id:guid}/import", async (Guid id, BulkImportQuestionsRequest request, IInterviewTopicService service, CancellationToken ct) =>
        {
            var result = await service.BulkImportQuestionsAsync(id, request, ct);
            return Results.Ok(ApiResponse<BulkImportResultDto>.Ok(result, $"Import complete: {result.Created} created, {result.Skipped} skipped."));
        }).RequireAuthorization("AdminOnly");

        return app;
    }
}
