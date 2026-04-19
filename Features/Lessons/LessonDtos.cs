using skillexa_backend.Domain.Enums;

namespace skillexa_backend.Features.Lessons;

public sealed record UpsertLessonRequest(string Title, string? Summary, int OrderIndex, bool IsPublished, IReadOnlyList<ContentBlockRequest> ContentBlocks);

public sealed record ContentBlockRequest(int OrderIndex, ContentBlockType Type, string? Content, string? CodeContent, string? Language, string? FileName, CalloutVariant? CalloutVariant, string? ImageUrl, string? ImageAlt, string? ImageCaption, ImageWidth? ImageWidth);

public sealed record LessonDto(Guid Id, Guid CourseId, string Title, string? Summary, int OrderIndex, bool IsPublished, IReadOnlyList<ContentBlockDto> ContentBlocks);

public sealed record ContentBlockDto(Guid Id, int OrderIndex, string Type, string? Content, string? CodeContent, string? Language, string? FileName, string? CalloutVariant, string? ImageUrl, string? ImageAlt, string? ImageCaption, string? ImageWidth);

public sealed record LessonProgressRequest(bool IsCompleted);

public sealed record LessonProgressDto(
    Guid LessonId,
    Guid UserId,
    bool IsCompleted,
    DateTime? CompletedAtUtc,
    int CourseProgressPercent,
    int XpEarned,
    int NewTotalXp,
    int NewLevel,
    string LevelTitle,
    int NewStreak,
    IReadOnlyList<string> BadgesEarned);

public sealed record CourseLessonProgressDto(Guid LessonId, bool IsCompleted, DateTime? CompletedAtUtc);
