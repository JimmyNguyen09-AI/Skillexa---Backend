using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Models;

[Table("courses")]
public class Course
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [Column("price")]
    public decimal Price { get; set; } = 0;

    [Column("is_published")]
    public bool IsPublished { get; set; } = false;

    [Column("instructor_id")]
    public Guid InstructorId { get; set; }

    [Column("category_id")]
    public Guid? CategoryId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(InstructorId))]
    public User Instructor { get; set; } = null!;

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
}