using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Models;

// ─── Enum ─────────────────────────────────────────────────────────────────────

// Mock: "Beginner" | "Intermediate" | "Advanced"
public enum CourseLevel
{
    Beginner     = 0,
    Intermediate = 1,
    Advanced     = 2,
}

// ─── Course ───────────────────────────────────────────────────────────────────

[Table("courses")]
public class Course
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Mock: title
    [Required]
    [MaxLength(300)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    // Mock: description
    [Column("description")]
    public string? Description { get; set; }

    // Mock: category — "Frontend" | "Language" | "Architecture" | "Backend" …
    [Required]
    [MaxLength(100)]
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    // Mock: level — "Beginner" | "Intermediate" | "Advanced"
    [Column("level")]
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;

    // Mock: duration — human-readable string e.g. "8h 30m"
    [MaxLength(30)]
    [Column("duration")]
    public string? Duration { get; set; }

    // Mock: enrolled — cached count, recomputed from Enrollments
    [Column("enrolled_count")]
    public int EnrolledCount { get; set; } = 0;

    [Column("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [Column("is_published")]
    public bool IsPublished { get; set; } = false;

    // Mock: instructor (name) — FK to users
    [Column("instructor_id")]
    public Guid InstructorId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(InstructorId))]
    public User Instructor { get; set; } = null!;

    public ICollection<Lesson>     Lessons     { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
}

// ─── Enrollment ───────────────────────────────────────────────────────────────
// Mock: Course.progress (per-user 0-100), UserRecord.enrolled / .completed

[Table("enrollments")]
public class Enrollment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("course_id")]
    public Guid CourseId { get; set; }

    // Mock: Course.progress — 0-100, recomputed when a lesson is completed
    [Column("progress_percent")]
    public int ProgressPercent { get; set; } = 0;

    [Column("enrolled_at")]
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;
}