using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Domain.Entities;

[Table("comments")]
public sealed class Comment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("lesson_id")]
    public Guid LessonId { get; set; }

    [Column("parent_comment_id")]
    public Guid? ParentCommentId { get; set; }

    [Column("created_at_utc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Column("updated_at_utc")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;

    [ForeignKey(nameof(ParentCommentId))]
    public Comment? ParentComment { get; set; }

    public ICollection<Comment> Replies { get; set; } = [];
}
