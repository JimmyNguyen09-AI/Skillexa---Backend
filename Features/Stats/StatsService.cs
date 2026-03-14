using Microsoft.EntityFrameworkCore;
using skillexa_backend.Domain.Enums;
using skillexa_backend.Infrastructure.Data;

namespace skillexa_backend.Features.Stats;

public sealed class StatsService(AppDbContext dbContext) : IStatsService
{
    public async Task<DashboardStatsDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var totalPublishedCourses = await dbContext.Courses.CountAsync(x => x.IsPublished, cancellationToken);
        var totalActiveUsers = await dbContext.Users.CountAsync(x => x.Status == UserStatus.Active, cancellationToken);
        var totalQuizAttempts = await dbContext.QuizAttempts.CountAsync(cancellationToken);

        var enrollmentCount = await dbContext.Enrollments.CountAsync(cancellationToken);
        var completedEnrollmentCount = await dbContext.Enrollments.CountAsync(x => x.CompletedAtUtc != null, cancellationToken);
        var completionRate = enrollmentCount == 0 ? 0 : Math.Round((double)completedEnrollmentCount / enrollmentCount * 100, 2);

        var utcNow = DateTime.UtcNow;
        var startDate = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var monthlyActivity = Enumerable.Range(0, 6)
            .Select(offset => startDate.AddMonths(offset))
            .Select(month => new MonthlyActivityDto(month.Year, month.Month, 0, 0, 0, 0))
            .ToDictionary(x => $"{x.Year}-{x.Month}");

        foreach (var row in await dbContext.Users
                     .Where(x => x.CreatedAtUtc >= startDate)
                     .GroupBy(x => new { x.CreatedAtUtc.Year, x.CreatedAtUtc.Month })
                     .Select(x => new { x.Key.Year, x.Key.Month, Count = x.Count() })
                     .ToListAsync(cancellationToken))
        {
            monthlyActivity[$"{row.Year}-{row.Month}"] = monthlyActivity[$"{row.Year}-{row.Month}"] with { NewUsers = row.Count };
        }

        foreach (var row in await dbContext.Enrollments
                     .Where(x => x.EnrolledAtUtc >= startDate)
                     .GroupBy(x => new { x.EnrolledAtUtc.Year, x.EnrolledAtUtc.Month })
                     .Select(x => new { x.Key.Year, x.Key.Month, Count = x.Count() })
                     .ToListAsync(cancellationToken))
        {
            monthlyActivity[$"{row.Year}-{row.Month}"] = monthlyActivity[$"{row.Year}-{row.Month}"] with { Enrollments = row.Count };
        }

        foreach (var row in await dbContext.QuizAttempts
                     .Where(x => x.SubmittedAtUtc >= startDate)
                     .GroupBy(x => new { x.SubmittedAtUtc.Year, x.SubmittedAtUtc.Month })
                     .Select(x => new { x.Key.Year, x.Key.Month, Count = x.Count() })
                     .ToListAsync(cancellationToken))
        {
            monthlyActivity[$"{row.Year}-{row.Month}"] = monthlyActivity[$"{row.Year}-{row.Month}"] with { QuizAttempts = row.Count };
        }

        foreach (var row in await dbContext.Comments
                     .Where(x => x.CreatedAtUtc >= startDate)
                     .GroupBy(x => new { x.CreatedAtUtc.Year, x.CreatedAtUtc.Month })
                     .Select(x => new { x.Key.Year, x.Key.Month, Count = x.Count() })
                     .ToListAsync(cancellationToken))
        {
            monthlyActivity[$"{row.Year}-{row.Month}"] = monthlyActivity[$"{row.Year}-{row.Month}"] with { Comments = row.Count };
        }

        return new DashboardStatsDto(
            totalPublishedCourses,
            totalActiveUsers,
            completionRate,
            totalQuizAttempts,
            monthlyActivity.Values.OrderBy(x => x.Year).ThenBy(x => x.Month).ToList());
    }
}
