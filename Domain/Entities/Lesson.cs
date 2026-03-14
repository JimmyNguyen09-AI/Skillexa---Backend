using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Domain.Entities;

[Table("lessons")]
public sealed class Lesson
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("course_id")]
    public Guid CourseId { get; set; }

    [Required]
    [MaxLength(300)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Column("summary")]
    public string? Summary { get; set; }

    [Column("order_index")]
    public int OrderIndex { get; set; }

    [Column("is_published")]
    public bool IsPublished { get; set; }

    [Column("created_at_utc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Column("updated_at_utc")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;

    public ICollection<ContentBlock> ContentBlocks { get; set; } = [];
    public ICollection<LessonProgress> LessonProgresses { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public Quiz? Quiz { get; set; }
}
