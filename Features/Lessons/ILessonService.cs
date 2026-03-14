namespace skillexa_backend.Features.Lessons;

public interface ILessonService
{
    Task<IReadOnlyList<LessonDto>> GetLessonsByCourseAsync(Guid courseId, bool includeUnpublished, CancellationToken cancellationToken);
    Task<LessonDto> GetLessonByIdAsync(Guid lessonId, bool includeUnpublished, CancellationToken cancellationToken);
    Task<LessonDto> UpsertLessonAsync(Guid courseId, Guid? lessonId, UpsertLessonRequest request, CancellationToken cancellationToken);
    Task<LessonProgressDto> UpdateProgressAsync(Guid userId, Guid lessonId, LessonProgressRequest request, CancellationToken cancellationToken);
}
