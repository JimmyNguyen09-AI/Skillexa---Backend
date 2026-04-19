namespace skillexa_backend.Features.Quizzes;

public sealed record UpsertQuizRequest(string Title, IReadOnlyList<QuizQuestionRequest> Questions);

public sealed record QuizQuestionRequest(int OrderIndex, string Prompt, IReadOnlyList<string> Options, int CorrectOptionIndex, string? Explanation);

public sealed record QuizDto(Guid Id, Guid LessonId, string Title, IReadOnlyList<QuizQuestionDto> Questions);

public sealed record QuizQuestionDto(Guid Id, int OrderIndex, string Prompt, IReadOnlyList<string> Options, string? Explanation, int? CorrectOptionIndex);

public sealed record QuizAnswerRequest(Guid QuestionId, int SelectedOptionIndex);

public sealed record SubmitQuizRequest(IReadOnlyList<QuizAnswerRequest> Answers);

public sealed record QuizResultQuestionDto(Guid QuestionId, string Prompt, int SelectedOptionIndex, int CorrectOptionIndex, bool IsCorrect, string? Explanation);

public sealed record QuizResultDto(
    Guid AttemptId,
    Guid QuizId,
    int Score,
    int CorrectCount,
    int TotalQuestions,
    DateTime SubmittedAtUtc,
    IReadOnlyList<QuizResultQuestionDto> Questions,
    int XpEarned,
    int NewTotalXp,
    int NewLevel,
    string LevelTitle,
    int NewStreak,
    IReadOnlyList<string> BadgesEarned);
