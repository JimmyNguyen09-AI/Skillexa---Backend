using skillexa_backend.Domain.Enums;

namespace skillexa_backend.Features.Courses;

public sealed record CreateCourseRequest(string Title, string? Slug, string? Description, CourseLevel Level, IReadOnlyList<CourseCategory>? Categories, string? ThumbnailUrl, bool IsPublished, CourseAccessTier AccessTier);

public sealed record UpdateCourseRequest(string Title, string? Slug, string? Description, CourseLevel Level, IReadOnlyList<CourseCategory>? Categories, string? ThumbnailUrl, bool IsPublished, CourseAccessTier AccessTier);

public sealed record CourseSummaryDto(Guid Id, string Title, string Slug, string? Description, string Level, IReadOnlyList<string> Categories, string? ThumbnailUrl, bool IsPublished, string AccessTier, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, int LessonCount, int EnrollmentCount);

public sealed record CourseDetailDto(Guid Id, string Title, string Slug, string? Description, string Level, IReadOnlyList<string> Categories, string? ThumbnailUrl, bool IsPublished, string AccessTier, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, IReadOnlyList<CourseLessonDto> Lessons, int EnrollmentCount);

public sealed record CourseLessonDto(Guid Id, string Title, string? Summary, int OrderIndex, bool IsPublished);

public sealed record EnrollmentDto(Guid EnrollmentId, Guid CourseId, Guid UserId, int ProgressPercent, DateTime EnrolledAtUtc, DateTime? CompletedAtUtc);

public sealed record MyLearningCourseDto(
    Guid EnrollmentId,
    Guid CourseId,
    string Title,
    string Slug,
    string? Description,
    string Level,
    IReadOnlyList<string> Categories,
    string? ThumbnailUrl,
    bool IsPublished,
    string AccessTier,
    int ProgressPercent,
    int LessonCount,
    DateTime EnrolledAtUtc,
    DateTime? CompletedAtUtc);
