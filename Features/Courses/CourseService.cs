using System.Net;
using Microsoft.EntityFrameworkCore;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Domain.Entities;
using skillexa_backend.Infrastructure.Data;

namespace skillexa_backend.Features.Courses;

public sealed class CourseService(AppDbContext dbContext) : ICourseService
{
    public async Task<IReadOnlyList<CourseSummaryDto>> GetCoursesAsync(bool includeUnpublished, CancellationToken cancellationToken)
    {
        var query = dbContext.Courses.AsQueryable();
        if (!includeUnpublished)
        {
            query = query.Where(x => x.IsPublished);
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new CourseSummaryDto(
                x.Id,
                x.Title,
                x.Slug,
                x.Description,
                x.Level.ToString(),
                x.ThumbnailUrl,
                x.IsPublished,
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                x.Lessons.Count,
                x.Enrollments.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<CourseDetailDto> GetCourseBySlugAsync(string slug, bool includeUnpublished, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses
            .Include(x => x.Lessons)
            .Include(x => x.Enrollments)
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken)
            ?? throw new AppException("Course was not found.", HttpStatusCode.NotFound);

        if (!includeUnpublished && !course.IsPublished)
        {
            throw new AppException("Course was not found.", HttpStatusCode.NotFound);
        }

        return Map(course);
    }

    public async Task<CourseDetailDto> CreateCourseAsync(CreateCourseRequest request, CancellationToken cancellationToken)
    {
        ValidateCourseRequest(request.Title);
        var slug = await EnsureUniqueSlugAsync(request.Slug, request.Title, null, cancellationToken);

        var course = new Course
        {
            Title = request.Title.Trim(),
            Slug = slug,
            Description = NormalizeOptional(request.Description),
            Level = request.Level,
            ThumbnailUrl = NormalizeOptional(request.ThumbnailUrl),
            IsPublished = request.IsPublished
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(course);
    }

    public async Task<CourseDetailDto> UpdateCourseAsync(Guid courseId, UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        ValidateCourseRequest(request.Title);

        var course = await dbContext.Courses
            .Include(x => x.Lessons)
            .Include(x => x.Enrollments)
            .FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken)
            ?? throw new AppException("Course was not found.", HttpStatusCode.NotFound);

        course.Title = request.Title.Trim();
        course.Slug = await EnsureUniqueSlugAsync(request.Slug, request.Title, course.Id, cancellationToken);
        course.Description = NormalizeOptional(request.Description);
        course.Level = request.Level;
        course.ThumbnailUrl = NormalizeOptional(request.ThumbnailUrl);
        course.IsPublished = request.IsPublished;
        course.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(course);
    }

    public async Task<EnrollmentDto> EnrollAsync(Guid userId, Guid courseId, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken)
            ?? throw new AppException("Course was not found.", HttpStatusCode.NotFound);

        if (!course.IsPublished)
        {
            throw new AppException("Course is not open for enrollment.");
        }

        var existing = await dbContext.Enrollments
            .FirstOrDefaultAsync(x => x.UserId == userId && x.CourseId == courseId, cancellationToken);

        if (existing is not null)
        {
            return Map(existing);
        }

        var enrollment = new Enrollment
        {
            UserId = userId,
            CourseId = courseId
        };

        dbContext.Enrollments.Add(enrollment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(enrollment);
    }

    private async Task<string> EnsureUniqueSlugAsync(string? requestedSlug, string title, Guid? currentCourseId, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(string.IsNullOrWhiteSpace(requestedSlug) ? title : requestedSlug);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            throw new AppException("Course slug is invalid.");
        }

        var slug = baseSlug;
        var suffix = 1;
        while (await dbContext.Courses.AnyAsync(x => x.Slug == slug && x.Id != currentCourseId, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    private static string Slugify(string value)
    {
        var chars = value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }

    private static void ValidateCourseRequest(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new AppException("Course title is required.");
        }
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CourseDetailDto Map(Course course)
        => new(
            course.Id,
            course.Title,
            course.Slug,
            course.Description,
            course.Level.ToString(),
            course.ThumbnailUrl,
            course.IsPublished,
            course.CreatedAtUtc,
            course.UpdatedAtUtc,
            course.Lessons.OrderBy(x => x.OrderIndex).Select(x => new CourseLessonDto(x.Id, x.Title, x.Summary, x.OrderIndex, x.IsPublished)).ToList(),
            course.Enrollments.Count);

    private static EnrollmentDto Map(Enrollment enrollment)
        => new(enrollment.Id, enrollment.CourseId, enrollment.UserId, enrollment.ProgressPercent, enrollment.EnrolledAtUtc, enrollment.CompletedAtUtc);
}
