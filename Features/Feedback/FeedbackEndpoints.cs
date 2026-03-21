using skillexa_backend.Common.Results;

namespace skillexa_backend.Features.Feedback;

public static class FeedbackEndpoints
{
    public static IEndpointRouteBuilder MapFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/feedback").WithTags("Feedback");

        group.MapPost("/", async (FeedbackRequest request, IFeedbackService service, CancellationToken cancellationToken) =>
        {
            await service.SubmitAsync(request, cancellationToken);
            return Results.Ok(ApiResponse<string>.Ok("ok", "Feedback sent."));
        }).AllowAnonymous();

        group.MapGet("/", async (IFeedbackService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetAllAsync(cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyList<FeedbackSummaryDto>>.Ok(result));
        }).RequireAuthorization("AdminOnly");

        return app;
    }
}
