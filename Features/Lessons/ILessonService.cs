namespace skillexa_backend.Features.Lessons;

public interface ILessonService
{
    Task<IReadOnlyList<LessonDto>> GetLessonsByCourseAsync(Guid courseId, bool includeUnpublished, CancellationToken cancellationToken);
    Task<LessonDto> GetLessonByIdAsync(Guid lessonId, bool includeUnpublished, CancellationToken cancellationToken);
    Task<LessonDto> UpsertLessonAsync(Guid courseId, Guid? lessonId, UpsertLessonRequest request, CancellationToken cancellationToken);
    Task DeleteLessonAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CourseLessonProgressDto>> GetCourseProgressAsync(Guid userId, Guid courseId, CancellationToken cancellationToken);
    Task<LessonProgressDto> UpdateProgressAsync(Guid userId, Guid lessonId, LessonProgressRequest request, CancellationToken cancellationToken);
}
