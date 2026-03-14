using System.Net;
using Microsoft.EntityFrameworkCore;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Domain.Entities;
using skillexa_backend.Infrastructure.Data;

namespace skillexa_backend.Features.Comments;

public sealed class CommentService(AppDbContext dbContext) : ICommentService
{
    public async Task<IReadOnlyList<CommentDto>> GetLessonCommentsAsync(Guid lessonId, CancellationToken cancellationToken)
    {
        var comments = await dbContext.Comments
            .Include(x => x.User)
            .Where(x => x.LessonId == lessonId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return BuildTree(comments);
    }

    public async Task<CommentDto> CreateAsync(Guid userId, Guid lessonId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new AppException("Comment content is required.");
        }

        if (!await dbContext.Lessons.AnyAsync(x => x.Id == lessonId, cancellationToken))
        {
            throw new AppException("Lesson was not found.", HttpStatusCode.NotFound);
        }

        if (request.ParentCommentId is Guid parentId)
        {
            var parentExists = await dbContext.Comments.AnyAsync(x => x.Id == parentId && x.LessonId == lessonId, cancellationToken);
            if (!parentExists)
            {
                throw new AppException("Parent comment was not found.", HttpStatusCode.NotFound);
            }
        }

        var user = await dbContext.Users.FirstAsync(x => x.Id == userId, cancellationToken);
        var comment = new Comment
        {
            UserId = userId,
            LessonId = lessonId,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content.Trim()
        };

        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CommentDto(comment.Id, lessonId, userId, user.Name, comment.Content, comment.ParentCommentId, comment.CreatedAtUtc, comment.UpdatedAtUtc, []);
    }

    public async Task<CommentDto> UpdateAsync(Guid userId, bool isAdmin, Guid commentId, UpdateCommentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new AppException("Comment content is required.");
        }

        var comment = await dbContext.Comments
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == commentId, cancellationToken)
            ?? throw new AppException("Comment was not found.", HttpStatusCode.NotFound);

        if (!isAdmin && comment.UserId != userId)
        {
            throw new AppException("You cannot edit this comment.", HttpStatusCode.Forbidden);
        }

        comment.Content = request.Content.Trim();
        comment.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CommentDto(comment.Id, comment.LessonId, comment.UserId, comment.User.Name, comment.Content, comment.ParentCommentId, comment.CreatedAtUtc, comment.UpdatedAtUtc, []);
    }

    public async Task DeleteAsync(Guid userId, bool isAdmin, Guid commentId, CancellationToken cancellationToken)
    {
        var comment = await dbContext.Comments.FirstOrDefaultAsync(x => x.Id == commentId, cancellationToken)
            ?? throw new AppException("Comment was not found.", HttpStatusCode.NotFound);

        if (!isAdmin && comment.UserId != userId)
        {
            throw new AppException("You cannot delete this comment.", HttpStatusCode.Forbidden);
        }

        dbContext.Comments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<CommentDto> BuildTree(IReadOnlyCollection<Comment> comments)
    {
        var byParent = comments
            .GroupBy(x => x.ParentCommentId)
            .ToDictionary(x => x.Key?.ToString() ?? "root", x => x.OrderBy(c => c.CreatedAtUtc).ToList());

        List<CommentDto> Build(Guid? parentId)
        {
            var key = parentId?.ToString() ?? "root";
            if (!byParent.TryGetValue(key, out var nodes))
            {
                return [];
            }

            return nodes.Select(node => new CommentDto(
                node.Id,
                node.LessonId,
                node.UserId,
                node.User.Name,
                node.Content,
                node.ParentCommentId,
                node.CreatedAtUtc,
                node.UpdatedAtUtc,
                Build(node.Id))).ToList();
        }

        return Build(null);
    }
}
