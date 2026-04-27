using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Domain.Entities;

[Table("interview_topics")]
public sealed class InterviewTopic
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(220)]
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [Column("is_published")]
    public bool IsPublished { get; set; }

    [Column("order_index")]
    public int OrderIndex { get; set; }

    [Column("created_at_utc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Column("updated_at_utc")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<InterviewPractice> Questions { get; set; } = [];
}
