using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Domain.Entities;

[Table("quiz_questions")]
public sealed class QuizQuestion
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("quiz_id")]
    public Guid QuizId { get; set; }

    [Column("order_index")]
    public int OrderIndex { get; set; }

    [Required]
    [Column("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [Required]
    [Column("options", TypeName = "jsonb")]
    public List<string> Options { get; set; } = [];

    [Column("correct_option_index")]
    public int CorrectOptionIndex { get; set; }

    [Column("explanation")]
    public string? Explanation { get; set; }

    [ForeignKey(nameof(QuizId))]
    public Quiz Quiz { get; set; } = null!;

    public ICollection<UserAnswer> UserAnswers { get; set; } = [];
}
