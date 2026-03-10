using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Models;

// ─── Enums ────────────────────────────────────────────────────────────────────

// Mock: "Admin" | "Trainer" | "Employee"
public enum UserRole
{
    Employee  = 0,
    Trainer   = 1,
    Admin     = 2,
}

// Mock: "Active" | "Inactive"
public enum UserStatus
{
    Inactive = 0,
    Active   = 1,
}

// ─── User ─────────────────────────────────────────────────────────────────────

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(320)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("password_hash")]
    public string? PasswordHash { get; set; }

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("role")]
    public UserRole Role { get; set; } = UserRole.Employee;

    // Mock: "Active" | "Inactive"
    [Column("status")]
    public UserStatus Status { get; set; } = UserStatus.Active;
    
    [Column("join_date")]
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Course>       CoursesAsInstructor { get; set; } = [];
    public ICollection<Enrollment>   Enrollments         { get; set; } = [];
    public ICollection<QuizAttempt>  QuizAttempts        { get; set; } = [];
    public ICollection<Comment>      Comments            { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens       { get; set; } = [];
}

// ─── RefreshToken ─────────────────────────────────────────────────────────────

[Table("refresh_tokens")]
public class RefreshToken
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("token")]
    public string Token { get; set; } = string.Empty;

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("revoked")]
    public bool Revoked { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}