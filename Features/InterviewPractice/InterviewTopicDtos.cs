using skillexa_backend.Domain.Enums;

namespace skillexa_backend.Features.InterviewPractice;

public sealed record CreateInterviewTopicRequest(
    string Title,
    string? Slug,
    string? Description,
    string? ThumbnailUrl,
    bool IsPublished,
    int OrderIndex);

public sealed record UpdateInterviewTopicRequest(
    string Title,
    string? Slug,
    string? Description,
    string? ThumbnailUrl,
    bool IsPublished,
    int OrderIndex);

public sealed record InterviewTopicSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string? ThumbnailUrl,
    bool IsPublished,
    int OrderIndex,
    int QuestionCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record InterviewTopicDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string? ThumbnailUrl,
    bool IsPublished,
    int OrderIndex,
    IReadOnlyList<InterviewPracticeSummaryDto> Questions,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record BulkImportQuestionsRequest(
    IReadOnlyList<ImportQuestionItem> Questions);

public sealed record ImportQuestionItem(
    string Title,
    string? Slug,
    string Question,
    CourseLevel Level,
    bool IsPublished,
    int OrderIndex,
    IReadOnlyList<InterviewPracticeContentBlockRequest>? ContentBlocks);

public sealed record BulkImportResultDto(
    int Created,
    int Skipped,
    IReadOnlyList<string> Errors);
