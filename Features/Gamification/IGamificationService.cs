namespace skillexa_backend.Features.Gamification;

public interface IGamificationService
{
    Task<GamificationProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task<XpAwardDto> AwardLessonXpAsync(Guid userId, CancellationToken cancellationToken);
    Task<XpAwardDto> AwardQuizXpAsync(Guid userId, int correctCount, int totalQuestions, CancellationToken cancellationToken);
}
