namespace skillexa_backend.Features.Gamification;

public sealed record GamificationProfileDto(
    int Xp,
    int Level,
    string LevelTitle,
    int XpInLevel,
    int XpToNextLevel,
    int StreakDays,
    IReadOnlyList<string> Badges);

public sealed record XpAwardDto(
    int XpEarned,
    int NewTotalXp,
    int NewLevel,
    string LevelTitle,
    int NewStreak,
    IReadOnlyList<string> BadgesEarned);
