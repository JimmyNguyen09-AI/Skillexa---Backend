namespace skillexa_backend.Features.Roadmaps;

public sealed record RoadmapNextCourseDto(Guid CourseId, string Title, string Slug, int OrderIndex);

public sealed record RoadmapCourseProgressDto(
    Guid CourseId,
    string Title,
    string Slug,
    string? Description,
    string Level,
    string? ThumbnailUrl,
    int OrderIndex,
    int LessonCount,
    bool IsPublished,
    bool IsEnrolled,
    bool IsCompleted,
    int ProgressPercent);

public sealed record RoadmapSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    int TotalCourses,
    int CompletedCourses,
    int ProgressPercent,
    string CurrentStage,
    RoadmapNextCourseDto? NextCourse);

public sealed record RoadmapDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    int TotalCourses,
    int CompletedCourses,
    int ProgressPercent,
    string CurrentStage,
    RoadmapNextCourseDto? NextCourse,
    IReadOnlyList<RoadmapCourseProgressDto> Courses);
