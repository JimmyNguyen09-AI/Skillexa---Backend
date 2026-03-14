using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Domain.Entities;

[Table("quiz_attempts")]
public sealed class QuizAttempt
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("quiz_id")]
    public Guid QuizId { get; set; }

    [Column("score")]
    public int Score { get; set; }

    [Column("correct_count")]
    public int CorrectCount { get; set; }

    [Column("total_questions")]
    public int TotalQuestions { get; set; }

    [Column("started_at_utc")]
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    [Column("submitted_at_utc")]
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(QuizId))]
    public Quiz Quiz { get; set; } = null!;

    public ICollection<UserAnswer> UserAnswers { get; set; } = [];
}
