using System.Net;
using Microsoft.EntityFrameworkCore;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Domain.Entities;
using skillexa_backend.Infrastructure.Data;

namespace skillexa_backend.Features.Gamification;

public sealed class GamificationService(AppDbContext dbContext) : IGamificationService
{
    // XP thresholds per level (index = level - 1, so index 0 = Level 1 = 0 XP required)
    private static readonly int[] LevelThresholds = [0, 200, 500, 1000, 1800, 3000, 4500, 6500, 9000, 12000];
    private static readonly string[] LevelTitles =
    [
        "Newbie", "Explorer", "Learner", "Practitioner", "Developer",
        "Builder", "Coder", "Engineer", "Architect", "Master"
    ];

    // Badge IDs
    private const string BadgeFirstLesson = "first_lesson";
    private const string BadgeFirstQuiz = "first_quiz";
    private const string BadgePerfectScore = "perfect_score";
    private const string BadgeStreak3 = "streak_3";
    private const string BadgeStreak7 = "streak_7";
    private const string BadgeStreak30 = "streak_30";
    private const string BadgeXp500 = "xp_500";
    private const string BadgeXp1000 = "xp_1000";

    public async Task<GamificationProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AppException("User not found.", HttpStatusCode.NotFound);

        return BuildProfile(user);
    }

    public async Task<XpAwardDto> AwardLessonXpAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AppException("User not found.", HttpStatusCode.NotFound);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var newBadges = new List<string>();

        var streakBonus = UpdateStreak(user, today);
        var xpEarned = 50 + streakBonus;
        user.Xp += xpEarned;

        // First lesson badge
        var lessonCount = await dbContext.LessonProgresses
            .CountAsync(x => x.UserId == userId && x.IsCompleted, cancellationToken);
        if (lessonCount == 1 && !user.Badges.Contains(BadgeFirstLesson))
        {
            user.Badges.Add(BadgeFirstLesson);
            newBadges.Add(BadgeFirstLesson);
        }

        CheckXpBadges(user, newBadges);
        CheckStreakBadges(user, newBadges);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildAward(user, xpEarned, newBadges);
    }

    public async Task<XpAwardDto> AwardQuizXpAsync(Guid userId, int correctCount, int totalQuestions, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AppException("User not found.", HttpStatusCode.NotFound);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var newBadges = new List<string>();

        var streakBonus = UpdateStreak(user, today);
        var scorePercent = totalQuestions > 0 ? (double)correctCount / totalQuestions : 0;
        var baseXp = (int)(20 + 80 * scorePercent);
        var perfectBonus = scorePercent >= 1.0 ? 50 : 0;
        var xpEarned = baseXp + perfectBonus + streakBonus;
        user.Xp += xpEarned;

        // First quiz badge
        var attemptCount = await dbContext.QuizAttempts
            .CountAsync(x => x.UserId == userId, cancellationToken);
        if (attemptCount == 1 && !user.Badges.Contains(BadgeFirstQuiz))
        {
            user.Badges.Add(BadgeFirstQuiz);
            newBadges.Add(BadgeFirstQuiz);
        }

        // Perfect score badge
        if (perfectBonus > 0 && !user.Badges.Contains(BadgePerfectScore))
        {
            user.Badges.Add(BadgePerfectScore);
            newBadges.Add(BadgePerfectScore);
        }

        CheckXpBadges(user, newBadges);
        CheckStreakBadges(user, newBadges);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildAward(user, xpEarned, newBadges);
    }

    private static int UpdateStreak(User user, DateOnly today)
    {
        var yesterday = today.AddDays(-1);
        int streakBonus = 0;

        if (user.LastStreakDateUtc == today)
        {
            // Already counted today — no change
            return 0;
        }
        else if (user.LastStreakDateUtc == yesterday)
        {
            user.StreakDays++;
            streakBonus = 20;
        }
        else
        {
            user.StreakDays = 1;
            streakBonus = 0;
        }

        user.LastStreakDateUtc = today;
        return streakBonus;
    }

    private static void CheckXpBadges(User user, List<string> newBadges)
    {
        if (user.Xp >= 500 && !user.Badges.Contains(BadgeXp500))
        {
            user.Badges.Add(BadgeXp500);
            newBadges.Add(BadgeXp500);
        }
        if (user.Xp >= 1000 && !user.Badges.Contains(BadgeXp1000))
        {
            user.Badges.Add(BadgeXp1000);
            newBadges.Add(BadgeXp1000);
        }
    }

    private static void CheckStreakBadges(User user, List<string> newBadges)
    {
        if (user.StreakDays >= 3 && !user.Badges.Contains(BadgeStreak3))
        {
            user.Badges.Add(BadgeStreak3);
            newBadges.Add(BadgeStreak3);
        }
        if (user.StreakDays >= 7 && !user.Badges.Contains(BadgeStreak7))
        {
            user.Badges.Add(BadgeStreak7);
            newBadges.Add(BadgeStreak7);
        }
        if (user.StreakDays >= 30 && !user.Badges.Contains(BadgeStreak30))
        {
            user.Badges.Add(BadgeStreak30);
            newBadges.Add(BadgeStreak30);
        }
    }

    private static (int level, int xpInLevel, int xpToNextLevel) ComputeLevel(int xp)
    {
        var level = 1;
        for (var i = LevelThresholds.Length - 1; i >= 0; i--)
        {
            if (xp >= LevelThresholds[i])
            {
                level = i + 1;
                break;
            }
        }

        var levelIndex = level - 1;
        var currentThreshold = LevelThresholds[levelIndex];
        var nextThreshold = levelIndex + 1 < LevelThresholds.Length
            ? LevelThresholds[levelIndex + 1]
            : LevelThresholds[^1] + 5000;

        return (level, xp - currentThreshold, nextThreshold - currentThreshold);
    }

    private static GamificationProfileDto BuildProfile(User user)
    {
        var (level, xpInLevel, xpToNextLevel) = ComputeLevel(user.Xp);
        var title = level <= LevelTitles.Length ? LevelTitles[level - 1] : LevelTitles[^1];
        return new GamificationProfileDto(user.Xp, level, title, xpInLevel, xpToNextLevel, user.StreakDays, user.Badges.AsReadOnly());
    }

    private static XpAwardDto BuildAward(User user, int xpEarned, List<string> newBadges)
    {
        var (level, _, _) = ComputeLevel(user.Xp);
        var title = level <= LevelTitles.Length ? LevelTitles[level - 1] : LevelTitles[^1];
        return new XpAwardDto(xpEarned, user.Xp, level, title, user.StreakDays, newBadges.AsReadOnly());
    }
}
