using skillexa_backend.Domain.Enums;

namespace skillexa_backend.Features.Courses;

public sealed record CreateCourseRequest(string Title, string? Slug, string? Description, CourseLevel Level, string? ThumbnailUrl, bool IsPublished);

public sealed record UpdateCourseRequest(string Title, string? Slug, string? Description, CourseLevel Level, string? ThumbnailUrl, bool IsPublished);

public sealed record CourseSummaryDto(Guid Id, string Title, string Slug, string? Description, string Level, string? ThumbnailUrl, bool IsPublished, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, int LessonCount, int EnrollmentCount);

public sealed record CourseDetailDto(Guid Id, string Title, string Slug, string? Description, string Level, string? ThumbnailUrl, bool IsPublished, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, IReadOnlyList<CourseLessonDto> Lessons, int EnrollmentCount);

public sealed record CourseLessonDto(Guid Id, string Title, string? Summary, int OrderIndex, bool IsPublished);

public sealed record EnrollmentDto(Guid EnrollmentId, Guid CourseId, Guid UserId, int ProgressPercent, DateTime EnrolledAtUtc, DateTime? CompletedAtUtc);
