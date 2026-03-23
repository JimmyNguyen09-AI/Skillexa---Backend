using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Domain.Entities;

[Table("roadmap_courses")]
public sealed class RoadmapCourse
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("roadmap_id")]
    public Guid RoadmapId { get; set; }

    [Column("course_id")]
    public Guid CourseId { get; set; }

    [Column("order_index")]
    public int OrderIndex { get; set; }

    [ForeignKey(nameof(RoadmapId))]
    public Roadmap Roadmap { get; set; } = null!;

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;
}
