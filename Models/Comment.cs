using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Models;

[Table("comments")]
public class Comment
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

    // Null = comment gốc, có giá trị = reply
    [Column("parent_id")]
    public Guid? ParentId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;

    [ForeignKey(nameof(ParentId))]
    public Comment? Parent { get; set; }

    public ICollection<Comment> Replies { get; set; } = [];
}