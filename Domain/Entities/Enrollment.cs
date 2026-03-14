using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Domain.Entities;

[Table("enrollments")]
public sealed class Enrollment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("course_id")]
    public Guid CourseId { get; set; }

    [Column("progress_percent")]
    public int ProgressPercent { get; set; }

    [Column("enrolled_at_utc")]
    public DateTime EnrolledAtUtc { get; set; } = DateTime.UtcNow;

    [Column("completed_at_utc")]
    public DateTime? CompletedAtUtc { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;
}
