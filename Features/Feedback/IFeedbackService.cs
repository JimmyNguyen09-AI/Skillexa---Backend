namespace skillexa_backend.Features.Feedback;

public interface IFeedbackService
{
    Task SubmitAsync(FeedbackRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<FeedbackSummaryDto>> GetAllAsync(CancellationToken cancellationToken);
}
