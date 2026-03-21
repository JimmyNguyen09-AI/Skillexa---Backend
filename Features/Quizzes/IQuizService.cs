namespace skillexa_backend.Features.Quizzes;

public interface IQuizService
{
    Task<QuizDto?> GetByLessonAsync(Guid lessonId, bool includeAnswers, CancellationToken cancellationToken);
    Task<QuizDto> UpsertAsync(Guid lessonId, UpsertQuizRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid lessonId, CancellationToken cancellationToken);
    Task<QuizResultDto> SubmitAsync(Guid userId, Guid lessonId, SubmitQuizRequest request, CancellationToken cancellationToken);
}
