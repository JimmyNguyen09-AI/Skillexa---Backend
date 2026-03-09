using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Models;

[Table("lessons")]
public class Lesson
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    public string? Content { get; set; }

    [Column("video_url")]
    public string? VideoUrl { get; set; }

    [Column("order")]
    public int Order { get; set; } = 0;

    [Column("duration_seconds")]
    public int DurationSeconds { get; set; } = 0;

    [Column("is_free_preview")]
    public bool IsFreePreview { get; set; } = false;

    [Column("course_id")]
    public Guid CourseId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;

    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Quiz> Quizzes { get; set; } = [];
}