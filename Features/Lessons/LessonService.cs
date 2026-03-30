using System.Net;
using Microsoft.EntityFrameworkCore;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Domain.Entities;
using skillexa_backend.Domain.Enums;
using skillexa_backend.Infrastructure.Data;

namespace skillexa_backend.Features.Lessons;

public sealed class LessonService(AppDbContext dbContext) : ILessonService
{
    public async Task<IReadOnlyList<LessonDto>> GetLessonsByCourseAsync(Guid courseId, bool includeUnpublished, Guid? viewerUserId, CancellationToken cancellationToken)
    {
        await EnsureCourseAccessAsync(courseId, viewerUserId, cancellationToken);

        var query = dbContext.Lessons
            .Include(x => x.ContentBlocks)
            .Where(x => x.CourseId == courseId);

        if (!includeUnpublished)
        {
            query = query.Where(x => x.IsPublished);
        }

        var lessons = await query.OrderBy(x => x.OrderIndex).ToListAsync(cancellationToken);
        return lessons.Select(Map).ToList();
    }

    public async Task<LessonDto> GetLessonByIdAsync(Guid lessonId, bool includeUnpublished, Guid? viewerUserId, CancellationToken cancellationToken)
    {
        var lesson = await dbContext.Lessons
            .Include(x => x.ContentBlocks)
            .FirstOrDefaultAsync(x => x.Id == lessonId, cancellationToken)
            ?? throw new AppException("Lesson was not found.", HttpStatusCode.NotFound);

        await EnsureCourseAccessAsync(lesson.CourseId, viewerUserId, cancellationToken);

        if (!includeUnpublished && !lesson.IsPublished)
        {
            throw new AppException("Lesson was not found.", HttpStatusCode.NotFound);
        }

        return Map(lesson);
    }

    public async Task<LessonDto> UpsertLessonAsync(Guid courseId, Guid? lessonId, UpsertLessonRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new AppException("Lesson title is required.");
        }

        if (request.OrderIndex <= 0)
        {
            throw new AppException("Lesson order index must be greater than 0.");
        }

        if (!await dbContext.Courses.AnyAsync(x => x.Id == courseId, cancellationToken))
        {
            throw new AppException("Course was not found.", HttpStatusCode.NotFound);
        }

        var duplicateOrderExists = await dbContext.Lessons.AnyAsync(
            x => x.CourseId == courseId && x.OrderIndex == request.OrderIndex && x.Id != lessonId,
            cancellationToken);

        if (duplicateOrderExists)
        {
            throw new AppException($"Lesson order {request.OrderIndex} already exists in this course.");
        }

        Lesson lesson;
        if (lessonId is null)
        {
            lesson = new Lesson { CourseId = courseId };
            dbContext.Lessons.Add(lesson);
        }
        else
        {
            lesson = await dbContext.Lessons
                .FirstOrDefaultAsync(x => x.Id == lessonId && x.CourseId == courseId, cancellationToken)
                ?? throw new AppException("Lesson was not found.", HttpStatusCode.NotFound);
        }

        lesson.Title = request.Title.Trim();
        lesson.Summary = string.IsNullOrWhiteSpace(request.Summary) ? null : request.Summary.Trim();
        lesson.OrderIndex = request.OrderIndex;
        lesson.IsPublished = request.IsPublished;
        lesson.UpdatedAtUtc = DateTime.UtcNow;

        if (lessonId is not null)
        {
            await dbContext.ContentBlocks
                .Where(x => x.LessonId == lesson.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }

        var contentBlocks = request.ContentBlocks
            .OrderBy(x => x.OrderIndex)
            .Select(x => new ContentBlock
            {
                LessonId = lesson.Id,
                OrderIndex = x.OrderIndex,
                Type = x.Type,
                Content = x.Content,
                CodeContent = x.CodeContent,
                Language = x.Language,
                FileName = x.FileName,
                CalloutVariant = x.CalloutVariant,
                ImageUrl = x.ImageUrl,
                ImageAlt = x.ImageAlt,
                ImageCaption = x.ImageCaption,
                ImageWidth = x.ImageWidth
            })
            .ToList();

        if (contentBlocks.Count > 0)
        {
            dbContext.ContentBlocks.AddRange(contentBlocks);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        lesson.ContentBlocks = contentBlocks;
        return Map(lesson);
    }

    public async Task DeleteLessonAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken)
    {
        var lesson = await dbContext.Lessons
            .FirstOrDefaultAsync(x => x.Id == lessonId && x.CourseId == courseId, cancellationToken)
            ?? throw new AppException("Lesson was not found.", HttpStatusCode.NotFound);

        dbContext.Lessons.Remove(lesson);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CourseLessonProgressDto>> GetCourseProgressAsync(Guid userId, Guid courseId, CancellationToken cancellationToken)
    {
        await EnsureCourseAccessAsync(courseId, userId, cancellationToken);

        return await dbContext.LessonProgresses
            .Where(x => x.UserId == userId && x.Lesson.CourseId == courseId)
            .Select(x => new CourseLessonProgressDto(
                x.LessonId,
                x.IsCompleted,
                x.CompletedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<LessonProgressDto> UpdateProgressAsync(Guid userId, Guid lessonId, LessonProgressRequest request, CancellationToken cancellationToken)
    {
        var lesson = await dbContext.Lessons.FirstOrDefaultAsync(x => x.Id == lessonId, cancellationToken)
            ?? throw new AppException("Lesson was not found.", HttpStatusCode.NotFound);

        await EnsureCourseAccessAsync(lesson.CourseId, userId, cancellationToken);
        var enrollment = await EnsureEnrollmentAsync(userId, lesson.CourseId, cancellationToken);

        var progress = await dbContext.LessonProgresses
            .FirstOrDefaultAsync(x => x.UserId == userId && x.LessonId == lessonId, cancellationToken);

        if (progress is null)
        {
            progress = new LessonProgress { UserId = userId, LessonId = lessonId };
            dbContext.LessonProgresses.Add(progress);
        }

        progress.IsCompleted = request.IsCompleted;
        progress.CompletedAtUtc = request.IsCompleted ? DateTime.UtcNow : null;

        await dbContext.SaveChangesAsync(cancellationToken);

        var publishedLessonCount = await dbContext.Lessons.CountAsync(x => x.CourseId == lesson.CourseId && x.IsPublished, cancellationToken);
        var completedCount = await dbContext.LessonProgresses.CountAsync(
            x => x.UserId == userId && x.IsCompleted && x.Lesson.CourseId == lesson.CourseId && x.Lesson.IsPublished,
            cancellationToken);

        enrollment.ProgressPercent = publishedLessonCount == 0 ? 0 : completedCount * 100 / publishedLessonCount;
        enrollment.CompletedAtUtc = enrollment.ProgressPercent == 100 ? DateTime.UtcNow : null;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new LessonProgressDto(lessonId, userId, progress.IsCompleted, progress.CompletedAtUtc, enrollment.ProgressPercent);
    }

    private async Task EnsureCourseAccessAsync(Guid courseId, Guid? viewerUserId, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses
            .FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken)
            ?? throw new AppException("Course was not found.", HttpStatusCode.NotFound);

        if (course.AccessTier != CourseAccessTier.Pro)
        {
            return;
        }

        if (viewerUserId is null)
        {
            throw new AppException("This course is available for Pro members only.", HttpStatusCode.Forbidden);
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == viewerUserId.Value, cancellationToken)
            ?? throw new AppException("User was not found.", HttpStatusCode.NotFound);

        if (user.Role == UserRole.Admin || user.MembershipPlan == MembershipPlan.Pro)
        {
            return;
        }

        throw new AppException("This course is available for Pro members only.", HttpStatusCode.Forbidden);
    }

    private async Task<Enrollment> EnsureEnrollmentAsync(Guid userId, Guid courseId, CancellationToken cancellationToken)
    {
        var enrollment = await dbContext.Enrollments
            .FirstOrDefaultAsync(x => x.UserId == userId && x.CourseId == courseId, cancellationToken);

        if (enrollment is not null)
        {
            return enrollment;
        }

        enrollment = new Enrollment
        {
            UserId = userId,
            CourseId = courseId
        };

        dbContext.Enrollments.Add(enrollment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return enrollment;
    }

    private static LessonDto Map(Lesson lesson)
        => new(
            lesson.Id,
            lesson.CourseId,
            lesson.Title,
            lesson.Summary,
            lesson.OrderIndex,
            lesson.IsPublished,
            lesson.ContentBlocks.OrderBy(x => x.OrderIndex).Select(x => new ContentBlockDto(
                x.Id,
                x.OrderIndex,
                x.Type.ToString(),
                x.Content,
                x.CodeContent,
                x.Language,
                x.FileName,
                x.CalloutVariant?.ToString(),
                x.ImageUrl,
                x.ImageAlt,
                x.ImageCaption,
                x.ImageWidth?.ToString())).ToList());
}
