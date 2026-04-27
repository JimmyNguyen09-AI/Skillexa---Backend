using System.Net;
using Microsoft.EntityFrameworkCore;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Domain.Enums;
using skillexa_backend.Infrastructure.Data;
using InterviewTopicEntity = skillexa_backend.Domain.Entities.InterviewTopic;
using InterviewPracticeEntity = skillexa_backend.Domain.Entities.InterviewPractice;

namespace skillexa_backend.Features.InterviewPractice;

public sealed class InterviewTopicService(AppDbContext dbContext) : IInterviewTopicService
{
    public async Task<IReadOnlyList<InterviewTopicSummaryDto>> GetAllAsync(Guid userId, CancellationToken ct)
    {
        await InterviewPracticeService.EnsureProAccessAsync(userId, dbContext, ct);

        var isAdmin = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => x.Role == UserRole.Admin)
            .FirstOrDefaultAsync(ct);

        return await dbContext.InterviewTopics
            .Where(x => isAdmin || x.IsPublished)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new InterviewTopicSummaryDto(
                x.Id, x.Title, x.Slug, x.Description, x.ThumbnailUrl,
                x.IsPublished, x.OrderIndex,
                x.Questions.Count(q => isAdmin || q.IsPublished),
                x.CreatedAtUtc, x.UpdatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<InterviewTopicDetailDto> GetBySlugAsync(string slug, Guid userId, CancellationToken ct)
    {
        await InterviewPracticeService.EnsureProAccessAsync(userId, dbContext, ct);

        var isAdmin = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => x.Role == UserRole.Admin)
            .FirstOrDefaultAsync(ct);

        var topic = await dbContext.InterviewTopics
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Slug == slug, ct)
            ?? throw new AppException("Interview topic not found.", HttpStatusCode.NotFound);

        if (!isAdmin && !topic.IsPublished)
            throw new AppException("Interview topic not found.", HttpStatusCode.NotFound);

        return MapDetail(topic, isAdmin);
    }

    public async Task<InterviewTopicDetailDto> CreateAsync(CreateInterviewTopicRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppException("Title is required.");

        var slug = await EnsureUniqueTopicSlugAsync(request.Slug, request.Title, null, ct);

        var topic = new InterviewTopicEntity
        {
            Title = request.Title.Trim(),
            Slug = slug,
            Description = Normalize(request.Description),
            ThumbnailUrl = Normalize(request.ThumbnailUrl),
            IsPublished = request.IsPublished,
            OrderIndex = request.OrderIndex,
        };

        dbContext.InterviewTopics.Add(topic);
        await dbContext.SaveChangesAsync(ct);

        return MapDetail(topic, isAdmin: true);
    }

    public async Task<InterviewTopicDetailDto> UpdateAsync(Guid id, UpdateInterviewTopicRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppException("Title is required.");

        var topic = await dbContext.InterviewTopics
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Interview topic not found.", HttpStatusCode.NotFound);

        topic.Title = request.Title.Trim();
        topic.Slug = await EnsureUniqueTopicSlugAsync(request.Slug, request.Title, topic.Id, ct);
        topic.Description = Normalize(request.Description);
        topic.ThumbnailUrl = Normalize(request.ThumbnailUrl);
        topic.IsPublished = request.IsPublished;
        topic.OrderIndex = request.OrderIndex;
        topic.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);
        return MapDetail(topic, isAdmin: true);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var topic = await dbContext.InterviewTopics
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Interview topic not found.", HttpStatusCode.NotFound);

        dbContext.InterviewTopics.Remove(topic);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<BulkImportResultDto> BulkImportQuestionsAsync(Guid topicId, BulkImportQuestionsRequest request, CancellationToken ct)
    {
        var topic = await dbContext.InterviewTopics
            .FirstOrDefaultAsync(x => x.Id == topicId, ct)
            ?? throw new AppException("Interview topic not found.", HttpStatusCode.NotFound);

        var created = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var item in request.Questions)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.Question))
                {
                    errors.Add($"Skipped item with empty title or question.");
                    skipped++;
                    continue;
                }

                var slug = await InterviewPracticeService.EnsureUniqueSlugAsync(item.Slug, item.Title, null, dbContext, ct);

                var practice = new InterviewPracticeEntity
                {
                    TopicId = topicId,
                    Title = item.Title.Trim(),
                    Slug = slug,
                    Question = item.Question.Trim(),
                    Level = item.Level,
                    IsPublished = item.IsPublished,
                    OrderIndex = item.OrderIndex,
                };

                dbContext.InterviewPractices.Add(practice);
                await dbContext.SaveChangesAsync(ct);

                if (item.ContentBlocks is { Count: > 0 })
                {
                    InterviewPracticeService.UpsertBlocks(practice, item.ContentBlocks);
                    await dbContext.SaveChangesAsync(ct);
                }

                created++;
            }
            catch (Exception ex) when (ex is not AppException)
            {
                errors.Add($"Error importing '{item.Title}': {ex.Message}");
                skipped++;
            }
        }

        return new BulkImportResultDto(created, skipped, errors);
    }

    private async Task<string> EnsureUniqueTopicSlugAsync(string? requested, string title, Guid? currentId, CancellationToken ct)
    {
        var baseSlug = InterviewPracticeService.Slugify(string.IsNullOrWhiteSpace(requested) ? title : requested);
        if (string.IsNullOrWhiteSpace(baseSlug))
            throw new AppException("Topic slug is invalid.");

        var slug = baseSlug;
        var suffix = 1;
        while (await dbContext.InterviewTopics.AnyAsync(x => x.Slug == slug && x.Id != currentId, ct))
            slug = $"{baseSlug}-{suffix++}";

        return slug;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static InterviewTopicDetailDto MapDetail(InterviewTopicEntity t, bool isAdmin) => new(
        t.Id, t.Title, t.Slug, t.Description, t.ThumbnailUrl,
        t.IsPublished, t.OrderIndex,
        t.Questions
            .Where(q => isAdmin || q.IsPublished)
            .OrderBy(q => q.OrderIndex)
            .Select(q => new InterviewPracticeSummaryDto(
                q.Id, q.Title, q.Slug, q.Question,
                q.Level.ToString(), q.IsPublished, q.OrderIndex,
                q.CreatedAtUtc, q.UpdatedAtUtc))
            .ToList(),
        t.CreatedAtUtc, t.UpdatedAtUtc);
}
