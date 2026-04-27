namespace skillexa_backend.Features.InterviewPractice;

public interface IInterviewTopicService
{
    Task<IReadOnlyList<InterviewTopicSummaryDto>> GetAllAsync(Guid userId, CancellationToken ct);
    Task<InterviewTopicDetailDto> GetBySlugAsync(string slug, Guid userId, CancellationToken ct);
    Task<InterviewTopicDetailDto> CreateAsync(CreateInterviewTopicRequest request, CancellationToken ct);
    Task<InterviewTopicDetailDto> UpdateAsync(Guid id, UpdateInterviewTopicRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<BulkImportResultDto> BulkImportQuestionsAsync(Guid topicId, BulkImportQuestionsRequest request, CancellationToken ct);
}
