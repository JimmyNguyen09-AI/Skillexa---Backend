using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using skillexa_backend.Domain.Enums;

namespace skillexa_backend.Domain.Entities;

[Table("users")]
public sealed class User
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

    [Required]
    [MaxLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("role")]
    public UserRole Role { get; set; } = UserRole.User;

    [Column("status")]
    public UserStatus Status { get; set; } = UserStatus.Active;

    [Column("membership_plan")]
    public MembershipPlan MembershipPlan { get; set; } = MembershipPlan.Free;

    [Column("ai_agent_usage_count")]
    public int AiAgentUsageCount { get; set; }

    [Column("ai_agent_usage_date_utc")]
    public DateOnly? AiAgentUsageDateUtc { get; set; }

    [Column("created_at_utc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Column("updated_at_utc")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<LessonProgress> LessonProgresses { get; set; } = [];
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
