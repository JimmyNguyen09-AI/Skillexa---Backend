using System.Net;
using System.Text.RegularExpressions;
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
        var completedLessonCounts = await LoadCompletedLessonCountsAsync(userId, cancellationToken);

        return roadmaps
            .Select(x => BuildSummary(x, enrollments, completedLessonCounts, includeUnpublished))
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
        var completedLessonCounts = await LoadCompletedLessonCountsAsync(userId, cancellationToken);
        return BuildDetail(roadmap, enrollments, completedLessonCounts, includeUnpublished);
    }

    public async Task<RoadmapDetailDto> CreateAsync(Guid userId, UpsertRoadmapRequest request, bool includeUnpublished, CancellationToken cancellationToken)
    {
        ValidateRoadmapRequest(request);

        var roadmap = new Roadmap
        {
            Name = request.Name.Trim(),
            Slug = await EnsureUniqueSlugAsync(request.Slug, request.Name, null, cancellationToken),
            Description = NormalizeOptional(request.Description)
        };

        dbContext.Roadmaps.Add(roadmap);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetRoadmapBySlugAsync(userId, roadmap.Slug, includeUnpublished, cancellationToken);
    }

    public async Task<RoadmapDetailDto> UpdateAsync(Guid userId, Guid roadmapId, UpsertRoadmapRequest request, bool includeUnpublished, CancellationToken cancellationToken)
    {
        ValidateRoadmapRequest(request);

        var roadmap = await dbContext.Roadmaps
            .FirstOrDefaultAsync(x => x.Id == roadmapId, cancellationToken)
            ?? throw new AppException("Roadmap was not found.", HttpStatusCode.NotFound);

        roadmap.Name = request.Name.Trim();
        roadmap.Slug = await EnsureUniqueSlugAsync(request.Slug, request.Name, roadmap.Id, cancellationToken);
        roadmap.Description = NormalizeOptional(request.Description);
        roadmap.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRoadmapBySlugAsync(userId, roadmap.Slug, includeUnpublished, cancellationToken);
    }

    public async Task DeleteAsync(Guid roadmapId, CancellationToken cancellationToken)
    {
        var roadmap = await dbContext.Roadmaps
            .FirstOrDefaultAsync(x => x.Id == roadmapId, cancellationToken)
            ?? throw new AppException("Roadmap was not found.", HttpStatusCode.NotFound);

        dbContext.Roadmaps.Remove(roadmap);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RoadmapDetailDto> AddCourseAsync(Guid userId, Guid roadmapId, UpsertRoadmapCourseRequest request, bool includeUnpublished, CancellationToken cancellationToken)
    {
        if (request.CourseId == Guid.Empty)
        {
            throw new AppException("Course is required.");
        }

        var roadmap = await dbContext.Roadmaps
            .Include(x => x.RoadmapCourses)
            .FirstOrDefaultAsync(x => x.Id == roadmapId, cancellationToken)
            ?? throw new AppException("Roadmap was not found.", HttpStatusCode.NotFound);

        var course = await dbContext.Courses.FirstOrDefaultAsync(x => x.Id == request.CourseId, cancellationToken)
            ?? throw new AppException("Course was not found.", HttpStatusCode.NotFound);

        var mappings = roadmap.RoadmapCourses.OrderBy(x => x.OrderIndex).ToList();
        var desiredOrder = request.OrderIndex.GetValueOrDefault(mappings.Count + 1);
        desiredOrder = Math.Clamp(desiredOrder, 1, mappings.Count + (mappings.Any(x => x.CourseId == request.CourseId) ? 0 : 1));

        var existingMapping = mappings.FirstOrDefault(x => x.CourseId == request.CourseId);
        if (existingMapping is null)
        {
            foreach (var mapping in mappings.Where(x => x.OrderIndex >= desiredOrder))
            {
                mapping.OrderIndex++;
            }

            dbContext.RoadmapCourses.Add(new RoadmapCourse
            {
                RoadmapId = roadmap.Id,
                CourseId = course.Id,
                OrderIndex = desiredOrder
            });
        }
        else if (existingMapping.OrderIndex != desiredOrder)
        {
            if (desiredOrder < existingMapping.OrderIndex)
            {
                foreach (var mapping in mappings.Where(x => x.CourseId != course.Id && x.OrderIndex >= desiredOrder && x.OrderIndex < existingMapping.OrderIndex))
                {
                    mapping.OrderIndex++;
                }
            }
            else
            {
                foreach (var mapping in mappings.Where(x => x.CourseId != course.Id && x.OrderIndex > existingMapping.OrderIndex && x.OrderIndex <= desiredOrder))
                {
                    mapping.OrderIndex--;
                }
            }

            existingMapping.OrderIndex = desiredOrder;
        }

        roadmap.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRoadmapBySlugAsync(userId, roadmap.Slug, includeUnpublished, cancellationToken);
    }

    public async Task<RoadmapDetailDto> RemoveCourseAsync(Guid userId, Guid roadmapId, Guid courseId, bool includeUnpublished, CancellationToken cancellationToken)
    {
        var roadmap = await dbContext.Roadmaps
            .Include(x => x.RoadmapCourses)
            .FirstOrDefaultAsync(x => x.Id == roadmapId, cancellationToken)
            ?? throw new AppException("Roadmap was not found.", HttpStatusCode.NotFound);

        var mapping = roadmap.RoadmapCourses.FirstOrDefault(x => x.CourseId == courseId)
            ?? throw new AppException("This course is not part of the roadmap.", HttpStatusCode.NotFound);

        dbContext.RoadmapCourses.Remove(mapping);

        foreach (var remainingMapping in roadmap.RoadmapCourses
                     .Where(x => x.CourseId != courseId)
                     .OrderBy(x => x.OrderIndex)
                     .Select((item, index) => new { item, OrderIndex = index + 1 }))
        {
            remainingMapping.item.OrderIndex = remainingMapping.OrderIndex;
        }

        roadmap.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRoadmapBySlugAsync(userId, roadmap.Slug, includeUnpublished, cancellationToken);
    }

    private async Task<Dictionary<Guid, Enrollment>> LoadEnrollmentsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Enrollments
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.CourseId, cancellationToken);
    }

    private async Task<Dictionary<Guid, int>> LoadCompletedLessonCountsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.LessonProgresses
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsCompleted)
            .GroupBy(x => x.Lesson.CourseId)
            .Select(group => new { CourseId = group.Key, CompletedLessons = group.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.CompletedLessons, cancellationToken);
    }

    private async Task<string> EnsureUniqueSlugAsync(string? requestedSlug, string name, Guid? currentRoadmapId, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(string.IsNullOrWhiteSpace(requestedSlug) ? name : requestedSlug);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            throw new AppException("Roadmap slug is invalid.");
        }

        var slug = baseSlug;
        var suffix = 2;
        while (await dbContext.Roadmaps.AnyAsync(x => x.Slug == slug && (!currentRoadmapId.HasValue || x.Id != currentRoadmapId.Value), cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private static void ValidateRoadmapRequest(UpsertRoadmapRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new AppException("Roadmap name is required.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", string.Empty);
        normalized = Regex.Replace(normalized, @"\s+", "-");
        normalized = Regex.Replace(normalized, @"-+", "-");
        return normalized.Trim('-');
    }

    private static RoadmapSummaryDto BuildSummary(
        Roadmap roadmap,
        IReadOnlyDictionary<Guid, Enrollment> enrollments,
        IReadOnlyDictionary<Guid, int> completedLessonCounts,
        bool includeUnpublished)
    {
        var courses = GetVisibleRoadmapCourses(roadmap, enrollments, completedLessonCounts, includeUnpublished);
        var stats = CalculateRoadmapStats(courses);
        var nextCourse = courses.FirstOrDefault(x => !x.IsCompleted);

        return new RoadmapSummaryDto(
            roadmap.Id,
            roadmap.Name,
            roadmap.Slug,
            roadmap.Description ?? string.Empty,
            courses.Count,
            stats.CompletedCourses,
            stats.TotalLessons,
            stats.CompletedLessons,
            stats.ProgressPercent,
            RoadmapStageHelper.GetStage(stats.ProgressPercent),
            nextCourse is null ? null : new RoadmapNextCourseDto(nextCourse.CourseId, nextCourse.Title, nextCourse.Slug, nextCourse.OrderIndex));
    }

    private static RoadmapDetailDto BuildDetail(
        Roadmap roadmap,
        IReadOnlyDictionary<Guid, Enrollment> enrollments,
        IReadOnlyDictionary<Guid, int> completedLessonCounts,
        bool includeUnpublished)
    {
        var courses = GetVisibleRoadmapCourses(roadmap, enrollments, completedLessonCounts, includeUnpublished);
        var stats = CalculateRoadmapStats(courses);
        var nextCourse = courses.FirstOrDefault(x => !x.IsCompleted);

        return new RoadmapDetailDto(
            roadmap.Id,
            roadmap.Name,
            roadmap.Slug,
            roadmap.Description ?? string.Empty,
            courses.Count,
            stats.CompletedCourses,
            stats.TotalLessons,
            stats.CompletedLessons,
            stats.ProgressPercent,
            RoadmapStageHelper.GetStage(stats.ProgressPercent),
            nextCourse is null ? null : new RoadmapNextCourseDto(nextCourse.CourseId, nextCourse.Title, nextCourse.Slug, nextCourse.OrderIndex),
            courses);
    }

    private static List<RoadmapCourseProgressDto> GetVisibleRoadmapCourses(
        Roadmap roadmap,
        IReadOnlyDictionary<Guid, Enrollment> enrollments,
        IReadOnlyDictionary<Guid, int> completedLessonCounts,
        bool includeUnpublished)
    {
        return roadmap.RoadmapCourses
            .Where(x => includeUnpublished || x.Course.IsPublished)
            .OrderBy(x => x.OrderIndex)
            .Select(x =>
            {
                enrollments.TryGetValue(x.CourseId, out var enrollment);
                completedLessonCounts.TryGetValue(x.CourseId, out var completedLessonCount);
                var lessonCount = x.Course.Lessons.Count(lesson => includeUnpublished || lesson.IsPublished);
                var normalizedCompletedLessons = Math.Clamp(completedLessonCount, 0, lessonCount);
                var progressPercent = lessonCount == 0
                    ? (enrollment?.ProgressPercent ?? 0)
                    : (int)Math.Round(normalizedCompletedLessons * 100d / lessonCount, MidpointRounding.AwayFromZero);
                var isCompleted = lessonCount > 0
                    ? normalizedCompletedLessons >= lessonCount
                    : ((enrollment?.CompletedAtUtc is not null) || progressPercent >= 100);

                return new RoadmapCourseProgressDto(
                    x.CourseId,
                    x.Course.Title,
                    x.Course.Slug,
                    x.Course.Description,
                    x.Course.Level.ToString(),
                    x.Course.Category.ToString(),
                    x.Course.ThumbnailUrl,
                    x.OrderIndex,
                    lessonCount,
                    normalizedCompletedLessons,
                    x.Course.IsPublished,
                    enrollment is not null || normalizedCompletedLessons > 0,
                    isCompleted,
                    progressPercent);
            })
            .ToList();
    }

    private static (int TotalLessons, int CompletedLessons, int CompletedCourses, int ProgressPercent) CalculateRoadmapStats(
        IReadOnlyCollection<RoadmapCourseProgressDto> courses)
    {
        var totalLessons = courses.Sum(x => x.LessonCount);
        var completedLessons = courses.Sum(x => Math.Clamp(x.CompletedLessons, 0, x.LessonCount));
        var completedCourses = courses.Count(x => x.IsCompleted);
        var progressPercent = totalLessons == 0
            ? 0
            : (int)Math.Round(completedLessons * 100d / totalLessons, MidpointRounding.AwayFromZero);

        return (totalLessons, completedLessons, completedCourses, progressPercent);
    }
}
