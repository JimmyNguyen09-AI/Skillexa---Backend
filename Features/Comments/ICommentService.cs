namespace skillexa_backend.Features.Comments;

public interface ICommentService
{
    Task<IReadOnlyList<CommentDto>> GetLessonCommentsAsync(Guid lessonId, CancellationToken cancellationToken);
    Task<CommentDto> CreateAsync(Guid userId, Guid lessonId, CreateCommentRequest request, CancellationToken cancellationToken);
    Task<CommentDto> UpdateAsync(Guid userId, bool isAdmin, Guid commentId, UpdateCommentRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, bool isAdmin, Guid commentId, CancellationToken cancellationToken);
}
