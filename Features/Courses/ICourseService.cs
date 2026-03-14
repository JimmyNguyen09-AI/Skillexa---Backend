namespace skillexa_backend.Features.Courses;

public interface ICourseService
{
    Task<IReadOnlyList<CourseSummaryDto>> GetCoursesAsync(bool includeUnpublished, CancellationToken cancellationToken);
    Task<CourseDetailDto> GetCourseBySlugAsync(string slug, bool includeUnpublished, CancellationToken cancellationToken);
    Task<CourseDetailDto> CreateCourseAsync(CreateCourseRequest request, CancellationToken cancellationToken);
    Task<CourseDetailDto> UpdateCourseAsync(Guid courseId, UpdateCourseRequest request, CancellationToken cancellationToken);
    Task<EnrollmentDto> EnrollAsync(Guid userId, Guid courseId, CancellationToken cancellationToken);
}
