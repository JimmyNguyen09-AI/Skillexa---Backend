using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Models;

// ─── Quiz ─────────────────────────────────────────────────────────────────────

[Table("quizzes")]
public class Quiz
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    // Điểm tối thiểu để pass (0-100)
    [Column("pass_score")]
    public int PassScore { get; set; } = 70;

    [Column("lesson_id")]
    public Guid LessonId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;

    public ICollection<Question> Questions { get; set; } = [];
    public ICollection<QuizAttempt> Attempts { get; set; } = [];
}

// ─── Question ─────────────────────────────────────────────────────────────────

[Table("questions")]
public class Question
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("type")]
    public QuestionType Type { get; set; } = QuestionType.SingleChoice;

    // Lưu dạng JSON: ["Option A", "Option B", "Option C", "Option D"]
    [Column("options", TypeName = "jsonb")]
    public string Options { get; set; } = "[]";

    // Lưu dạng JSON: ["Option A"] hoặc ["Option A", "Option C"] nếu multiple
    [Column("correct_answers", TypeName = "jsonb")]
    public string CorrectAnswers { get; set; } = "[]";

    [Column("order")]
    public int Order { get; set; } = 0;

    [Column("quiz_id")]
    public Guid QuizId { get; set; }

    // Navigation
    [ForeignKey(nameof(QuizId))]
    public Quiz Quiz { get; set; } = null!;

    public ICollection<UserAnswer> UserAnswers { get; set; } = [];
}

public enum QuestionType
{
    SingleChoice,   // Chọn 1 đáp án
    MultipleChoice, // Chọn nhiều đáp án
    TrueFalse       // Đúng / Sai
}

// ─── QuizAttempt ──────────────────────────────────────────────────────────────

[Table("quiz_attempts")]
public class QuizAttempt
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("quiz_id")]
    public Guid QuizId { get; set; }

    [Column("score")]
    public float Score { get; set; } = 0;

    [Column("is_passed")]
    public bool IsPassed { get; set; } = false;

    [Column("started_at")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Column("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(QuizId))]
    public Quiz Quiz { get; set; } = null!;

    public ICollection<UserAnswer> UserAnswers { get; set; } = [];
}

// ─── UserAnswer ───────────────────────────────────────────────────────────────

[Table("user_answers")]
public class UserAnswer
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("attempt_id")]
    public Guid AttemptId { get; set; }

    [Column("question_id")]
    public Guid QuestionId { get; set; }

    // Lưu dạng JSON: đáp án user chọn
    [Column("selected_answers", TypeName = "jsonb")]
    public string SelectedAnswers { get; set; } = "[]";

    [Column("is_correct")]
    public bool IsCorrect { get; set; } = false;

    // Navigation
    [ForeignKey(nameof(AttemptId))]
    public QuizAttempt Attempt { get; set; } = null!;

    [ForeignKey(nameof(QuestionId))]
    public Question Question { get; set; } = null!;
}