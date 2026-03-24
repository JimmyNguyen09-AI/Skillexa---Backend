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
