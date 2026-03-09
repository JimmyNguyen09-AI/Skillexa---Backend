using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("password_hash")]
    public string? PasswordHash { get; set; }

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("role")]
    public UserRole Role { get; set; } = UserRole.Learner;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Course> CoursesCreated { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
}

public enum UserRole
{
    Learner,
    Instructor,
    Admin
}