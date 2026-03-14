namespace skillexa_backend.Features.Stats;

public sealed record DashboardStatsDto(int TotalPublishedCourses, int TotalActiveUsers, double CompletionRate, int TotalQuizAttempts, IReadOnlyList<MonthlyActivityDto> MonthlyActivity);

public sealed record MonthlyActivityDto(int Year, int Month, int NewUsers, int Enrollments, int QuizAttempts, int Comments);
