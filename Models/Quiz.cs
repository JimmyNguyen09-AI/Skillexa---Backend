using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Models;

// ─── Quiz ─────────────────────────────────────────────────────────────────────
// Mock: Quiz { id, lessonId, title, questions[] }
// One quiz per lesson (1-to-1)

[Table("quizzes")]
public class Quiz
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Mock: Quiz.title
    [Required]
    [MaxLength(300)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    // Mock: Quiz.lessonId — one-to-one with Lesson
    [Column("lesson_id")]
    public Guid LessonId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;

    // Mock: Quiz.questions[]
    public ICollection<QuizQuestion> Questions { get; set; } = [];

    public ICollection<QuizAttempt> Attempts { get; set; } = [];
}

// ─── QuizQuestion ─────────────────────────────────────────────────────────────
// Mock: QuizQuestion { id, text, options: string[], correctIndex: number, explanation: string }

[Table("quiz_questions")]
public class QuizQuestion
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("quiz_id")]
    public Guid QuizId { get; set; }

    // Display order within the quiz
    [Column("order")]
    public int Order { get; set; } = 0;

    // Mock: QuizQuestion.text
    [Required]
    [Column("text")]
    public string Text { get; set; } = string.Empty;

    // Mock: QuizQuestion.options — string[]
    // Stored as JSONB: ["Option A", "Option B", "Option C", "Option D"]
    [Required]
    [Column("options", TypeName = "jsonb")]
    public string Options { get; set; } = "[]";

    // Mock: QuizQuestion.correctIndex — 0-based index into options[]
    [Column("correct_index")]
    public int CorrectIndex { get; set; } = 0;

    // Mock: QuizQuestion.explanation — shown after submit
    [Required]
    [Column("explanation")]
    public string Explanation { get; set; } = string.Empty;

    // Navigation
    [ForeignKey(nameof(QuizId))]
    public Quiz Quiz { get; set; } = null!;

    public ICollection<UserAnswer> UserAnswers { get; set; } = [];
}

// ─── QuizAttempt ──────────────────────────────────────────────────────────────
// Mock: POST /api/lessons/:id/quiz/submit → QuizResult
//       body: { answers: { questionId, selectedIndex }[] }

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

    // Mock: score = count of correct answers
    [Column("score")]
    public int Score { get; set; } = 0;

    // Mock: total = quiz.questions.length
    [Column("total")]
    public int Total { get; set; } = 0;

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
// Mock: answers: { questionId: string; selectedIndex: number }[]

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

    // Mock: selectedIndex — 0-based index the user picked
    [Column("selected_index")]
    public int SelectedIndex { get; set; } = 0;

    // Computed at submit time: selectedIndex == question.correctIndex
    [Column("is_correct")]
    public bool IsCorrect { get; set; } = false;

    // Navigation
    [ForeignKey(nameof(AttemptId))]
    public QuizAttempt Attempt { get; set; } = null!;

    [ForeignKey(nameof(QuestionId))]
    public QuizQuestion Question { get; set; } = null!;
}