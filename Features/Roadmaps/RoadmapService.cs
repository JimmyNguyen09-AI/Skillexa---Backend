using System.Net;
using Microsoft.EntityFrameworkCore;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Domain.Entities;
using skillexa_backend.Infrastructure.Data;

namespace skillexa_backend.Features.Roadmaps;

public sealed class RoadmapService(AppDbContext dbContext) : IRoadmapService
{
    public async Task<IReadOnlyList<RoadmapSummaryDto>> GetRoadmapsAsync(Guid userId, bool includeUnpublished, CancellationToken cancellationToken)
    {
        var roadmaps = await dbContext.Roadmaps
            .AsNoTracking()
            .Include(x => x.RoadmapCourses)
                .ThenInclude(x => x.Course)
                    .ThenInclude(x => x.Lessons)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var enrollments = await LoadEnrollmentsAsync(userId, cancellationToken);
        return roadmaps
            .Select(x => BuildSummary(x, enrollments, includeUnpublished))
            .ToList();
    }

    public async Task<RoadmapDetailDto> GetRoadmapBySlugAsync(Guid userId, string slug, bool includeUnpublished, CancellationToken cancellationToken)
    {
        var roadmap = await dbContext.Roadmaps
            .AsNoTracking()
            .Include(x => x.RoadmapCourses)
                .ThenInclude(x => x.Course)
                    .ThenInclude(x => x.Lessons)
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken)
            ?? throw new AppException("Roadmap was not found.", HttpStatusCode.NotFound);

        var enrollments = await LoadEnrollmentsAsync(userId, cancellationToken);
        return BuildDetail(roadmap, enrollments, includeUnpublished);
    }

    private async Task<Dictionary<Guid, Enrollment>> LoadEnrollmentsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Enrollments
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.CourseId, cancellationToken);
    }

    private static RoadmapSummaryDto BuildSummary(Roadmap roadmap, IReadOnlyDictionary<Guid, Enrollment> enrollments, bool includeUnpublished)
    {
        var courses = GetVisibleRoadmapCourses(roadmap, enrollments, includeUnpublished);
        var progressPercent = CalculateProgressPercent(courses);
        var completedCourses = courses.Count(x => x.IsCompleted);
        var nextCourse = courses.FirstOrDefault(x => !x.IsCompleted);

        return new RoadmapSummaryDto(
            roadmap.Id,
            roadmap.Name,
            roadmap.Slug,
            roadmap.Description ?? string.Empty,
            courses.Count,
            completedCourses,
            progressPercent,
            RoadmapStageHelper.GetStage(progressPercent),
            nextCourse is null ? null : new RoadmapNextCourseDto(nextCourse.CourseId, nextCourse.Title, nextCourse.Slug, nextCourse.OrderIndex));
    }

    private static RoadmapDetailDto BuildDetail(Roadmap roadmap, IReadOnlyDictionary<Guid, Enrollment> enrollments, bool includeUnpublished)
    {
        var courses = GetVisibleRoadmapCourses(roadmap, enrollments, includeUnpublished);
        var progressPercent = CalculateProgressPercent(courses);
        var completedCourses = courses.Count(x => x.IsCompleted);
        var nextCourse = courses.FirstOrDefault(x => !x.IsCompleted);

        return new RoadmapDetailDto(
            roadmap.Id,
            roadmap.Name,
            roadmap.Slug,
            roadmap.Description ?? string.Empty,
            courses.Count,
            completedCourses,
            progressPercent,
            RoadmapStageHelper.GetStage(progressPercent),
            nextCourse is null ? null : new RoadmapNextCourseDto(nextCourse.CourseId, nextCourse.Title, nextCourse.Slug, nextCourse.OrderIndex),
            courses);
    }

    private static List<RoadmapCourseProgressDto> GetVisibleRoadmapCourses(Roadmap roadmap, IReadOnlyDictionary<Guid, Enrollment> enrollments, bool includeUnpublished)
    {
        return roadmap.RoadmapCourses
            .Where(x => includeUnpublished || x.Course.IsPublished)
            .OrderBy(x => x.OrderIndex)
            .Select(x =>
            {
                enrollments.TryGetValue(x.CourseId, out var enrollment);
                var progressPercent = enrollment?.ProgressPercent ?? 0;
                var isCompleted = (enrollment?.CompletedAtUtc is not null) || progressPercent >= 100;

                return new RoadmapCourseProgressDto(
                    x.CourseId,
                    x.Course.Title,
                    x.Course.Slug,
                    x.Course.Description,
                    x.Course.Level.ToString(),
                    x.Course.ThumbnailUrl,
                    x.OrderIndex,
                    x.Course.Lessons.Count(lesson => includeUnpublished || lesson.IsPublished),
                    x.Course.IsPublished,
                    enrollment is not null,
                    isCompleted,
                    progressPercent);
            })
            .ToList();
    }

    private static int CalculateProgressPercent(IReadOnlyCollection<RoadmapCourseProgressDto> courses)
    {
        if (courses.Count == 0)
        {
            return 0;
        }

        return (int)Math.Round(courses.Average(x => x.ProgressPercent), MidpointRounding.AwayFromZero);
    }
}
