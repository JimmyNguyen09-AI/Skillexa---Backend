namespace skillexa_backend.Features.Comments;

public sealed record CreateCommentRequest(string Content, Guid? ParentCommentId);

public sealed record UpdateCommentRequest(string Content);

public sealed record CommentDto(Guid Id, Guid LessonId, Guid UserId, string UserName, string Content, Guid? ParentCommentId, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, IReadOnlyList<CommentDto> Replies);
