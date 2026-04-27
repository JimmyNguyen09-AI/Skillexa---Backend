namespace skillexa_backend.Features.InterviewPractice;

public interface IInterviewPracticeService
{
    Task<IReadOnlyList<InterviewPracticeSummaryDto>> GetAllAsync(Guid userId, CancellationToken ct);
    Task<InterviewPracticeDetailDto> GetBySlugAsync(string slug, Guid userId, CancellationToken ct);
    Task<InterviewPracticeDetailDto> CreateAsync(CreateInterviewPracticeRequest request, CancellationToken ct);
    Task<InterviewPracticeDetailDto> UpdateAsync(Guid id, UpdateInterviewPracticeRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
