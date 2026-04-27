using skillexa_backend.Domain.Enums;

namespace skillexa_backend.Features.InterviewPractice;

public sealed record CreateInterviewPracticeRequest(
    Guid TopicId,
    string Title,
    string? Slug,
    string Question,
    CourseLevel Level,
    bool IsPublished,
    int OrderIndex,
    IReadOnlyList<InterviewPracticeContentBlockRequest> ContentBlocks);

public sealed record UpdateInterviewPracticeRequest(
    Guid TopicId,
    string Title,
    string? Slug,
    string Question,
    CourseLevel Level,
    bool IsPublished,
    int OrderIndex,
    IReadOnlyList<InterviewPracticeContentBlockRequest> ContentBlocks);

public sealed record InterviewPracticeContentBlockRequest(
    int OrderIndex,
    ContentBlockType Type,
    string? Content,
    string? CodeContent,
    string? Language,
    string? FileName,
    CalloutVariant? CalloutVariant,
    string? ImageUrl,
    string? ImageAlt,
    string? ImageCaption,
    ImageWidth? ImageWidth);

public sealed record InterviewPracticeContentBlockDto(
    Guid Id,
    int OrderIndex,
    string Type,
    string? Content,
    string? CodeContent,
    string? Language,
    string? FileName,
    string? CalloutVariant,
    string? ImageUrl,
    string? ImageAlt,
    string? ImageCaption,
    string? ImageWidth);

public sealed record InterviewPracticeSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string Question,
    string Level,
    bool IsPublished,
    int OrderIndex,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record InterviewPracticeDetailDto(
    Guid Id,
    Guid TopicId,
    string TopicSlug,
    string Title,
    string Slug,
    string Question,
    string Level,
    bool IsPublished,
    int OrderIndex,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<InterviewPracticeContentBlockDto> ContentBlocks);
