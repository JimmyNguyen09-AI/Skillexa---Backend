using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Domain.Entities;

[Table("roadmaps")]
public sealed class Roadmap
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(160)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(180)]
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at_utc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Column("updated_at_utc")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<RoadmapCourse> RoadmapCourses { get; set; } = [];
}
