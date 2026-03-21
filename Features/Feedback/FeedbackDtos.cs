namespace skillexa_backend.Features.Feedback;

public sealed record FeedbackRequest(string Name, string Email, string Message);

public sealed record FeedbackSummaryDto(Guid Id, string Name, string Email, string Message, DateTime CreatedAtUtc);
