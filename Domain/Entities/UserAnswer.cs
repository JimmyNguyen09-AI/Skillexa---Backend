using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Domain.Entities;

[Table("user_answers")]
public sealed class UserAnswer
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("attempt_id")]
    public Guid AttemptId { get; set; }

    [Column("question_id")]
    public Guid QuestionId { get; set; }

    [Column("selected_option_index")]
    public int SelectedOptionIndex { get; set; }

    [Column("is_correct")]
    public bool IsCorrect { get; set; }

    [ForeignKey(nameof(AttemptId))]
    public QuizAttempt Attempt { get; set; } = null!;

    [ForeignKey(nameof(QuestionId))]
    public QuizQuestion Question { get; set; } = null!;
}
