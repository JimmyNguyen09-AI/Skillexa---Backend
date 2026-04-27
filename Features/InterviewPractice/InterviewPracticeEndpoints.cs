using skillexa_backend.Common.Results;
using skillexa_backend.Infrastructure.Security;

namespace skillexa_backend.Features.InterviewPractice;

public static class InterviewPracticeEndpoints
{
    public static IEndpointRouteBuilder MapInterviewPracticeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/interview-practice").WithTags("InterviewPractice");

        group.MapGet("/", async (HttpContext httpContext, IInterviewPracticeService service, CancellationToken ct) =>
        {
            var result = await service.GetAllAsync(httpContext.User.GetRequiredUserId(), ct);
            return Results.Ok(ApiResponse<IReadOnlyList<InterviewPracticeSummaryDto>>.Ok(result));
        }).RequireAuthorization();

        group.MapGet("/{slug}", async (string slug, HttpContext httpContext, IInterviewPracticeService service, CancellationToken ct) =>
        {
            var result = await service.GetBySlugAsync(slug, httpContext.User.GetRequiredUserId(), ct);
            return Results.Ok(ApiResponse<InterviewPracticeDetailDto>.Ok(result));
        }).RequireAuthorization();

        group.MapPost("/", async (CreateInterviewPracticeRequest request, IInterviewPracticeService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return Results.Created($"/api/interview-practice/{result.Slug}", ApiResponse<InterviewPracticeDetailDto>.Ok(result, "Interview practice created."));
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/{id:guid}", async (Guid id, UpdateInterviewPracticeRequest request, IInterviewPracticeService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, request, ct);
            return Results.Ok(ApiResponse<InterviewPracticeDetailDto>.Ok(result, "Interview practice updated."));
        }).RequireAuthorization("AdminOnly");

        group.MapDelete("/{id:guid}", async (Guid id, IInterviewPracticeService service, CancellationToken ct) =>
        {
            await service.DeleteAsync(id, ct);
            return Results.Ok(ApiResponse<string>.Ok("ok", "Interview practice deleted."));
        }).RequireAuthorization("AdminOnly");

        return app;
    }
}
