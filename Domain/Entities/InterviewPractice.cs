using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using skillexa_backend.Domain.Enums;

namespace skillexa_backend.Domain.Entities;

[Table("interview_practices")]
public sealed class InterviewPractice
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(300)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(320)]
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [Column("question")]
    public string Question { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("level")]
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;

    [Column("categories")]
    public string[] Categories { get; set; } = [nameof(CourseCategory.Fundamentals)];

    [Column("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [Column("is_published")]
    public bool IsPublished { get; set; }

    [Column("created_at_utc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Column("updated_at_utc")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<InterviewPracticeContentBlock> ContentBlocks { get; set; } = [];
}
