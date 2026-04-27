using System.Net;
using Microsoft.EntityFrameworkCore;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Domain.Enums;
using skillexa_backend.Infrastructure.Data;
using InterviewPracticeEntity = skillexa_backend.Domain.Entities.InterviewPractice;
using InterviewPracticeContentBlock = skillexa_backend.Domain.Entities.InterviewPracticeContentBlock;

namespace skillexa_backend.Features.InterviewPractice;

public sealed class InterviewPracticeService(AppDbContext dbContext) : IInterviewPracticeService
{
    public async Task<InterviewPracticeDetailDto> GetBySlugAsync(string slug, Guid userId, CancellationToken ct)
    {
        await EnsureProAccessAsync(userId, ct);

        var isAdmin = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => x.Role == UserRole.Admin)
            .FirstOrDefaultAsync(ct);

        var practice = await dbContext.InterviewPractices
            .Include(x => x.ContentBlocks)
            .Include(x => x.Topic)
            .FirstOrDefaultAsync(x => x.Slug == slug, ct)
            ?? throw new AppException("Interview practice not found.", HttpStatusCode.NotFound);

        if (!isAdmin && !practice.IsPublished)
            throw new AppException("Interview practice not found.", HttpStatusCode.NotFound);

        return Map(practice);
    }

    public async Task<InterviewPracticeDetailDto> CreateAsync(CreateInterviewPracticeRequest request, CancellationToken ct)
    {
        ValidateRequest(request.Title, request.Question);

        var topicExists = await dbContext.InterviewTopics.AnyAsync(x => x.Id == request.TopicId, ct);
        if (!topicExists)
            throw new AppException("Interview topic not found.", HttpStatusCode.NotFound);

        var slug = await EnsureUniqueSlugAsync(request.Slug, request.Title, null, ct);

        var practice = new InterviewPracticeEntity
        {
            TopicId = request.TopicId,
            Title = request.Title.Trim(),
            Slug = slug,
            Question = request.Question.Trim(),
            Level = request.Level,
            IsPublished = request.IsPublished,
            OrderIndex = request.OrderIndex,
        };

        dbContext.InterviewPractices.Add(practice);
        await dbContext.SaveChangesAsync(ct);

        UpsertBlocks(practice, request.ContentBlocks ?? []);
        await dbContext.SaveChangesAsync(ct);

        await dbContext.Entry(practice).Reference(x => x.Topic).LoadAsync(ct);
        return Map(practice);
    }

    public async Task<InterviewPracticeDetailDto> UpdateAsync(Guid id, UpdateInterviewPracticeRequest request, CancellationToken ct)
    {
        ValidateRequest(request.Title, request.Question);

        var topicExists = await dbContext.InterviewTopics.AnyAsync(x => x.Id == request.TopicId, ct);
        if (!topicExists)
            throw new AppException("Interview topic not found.", HttpStatusCode.NotFound);

        var practice = await dbContext.InterviewPractices
            .Include(x => x.ContentBlocks)
            .Include(x => x.Topic)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Interview practice not found.", HttpStatusCode.NotFound);

        practice.TopicId = request.TopicId;
        practice.Title = request.Title.Trim();
        practice.Slug = await EnsureUniqueSlugAsync(request.Slug, request.Title, practice.Id, ct);
        practice.Question = request.Question.Trim();
        practice.Level = request.Level;
        practice.IsPublished = request.IsPublished;
        practice.OrderIndex = request.OrderIndex;
        practice.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.InterviewPracticeContentBlocks.RemoveRange(practice.ContentBlocks);
        UpsertBlocks(practice, request.ContentBlocks ?? []);

        await dbContext.SaveChangesAsync(ct);
        await dbContext.Entry(practice).Reference(x => x.Topic).LoadAsync(ct);
        return Map(practice);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var practice = await dbContext.InterviewPractices
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Interview practice not found.", HttpStatusCode.NotFound);

        dbContext.InterviewPractices.Remove(practice);
        await dbContext.SaveChangesAsync(ct);
    }

    internal static async Task EnsureProAccessAsync(Guid userId, AppDbContext dbContext, CancellationToken ct)
    {
        var user = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => new { x.Role, x.MembershipPlan })
            .FirstOrDefaultAsync(ct)
            ?? throw new AppException("User was not found.", HttpStatusCode.NotFound);

        if (user.Role != UserRole.Admin && user.MembershipPlan != MembershipPlan.Pro)
            throw new AppException("Interview Practice is available for Pro members only.", HttpStatusCode.Forbidden);
    }

    private async Task EnsureProAccessAsync(Guid userId, CancellationToken ct)
        => await EnsureProAccessAsync(userId, dbContext, ct);

    internal static void UpsertBlocks(InterviewPracticeEntity practice, IReadOnlyList<InterviewPracticeContentBlockRequest> blocks)
    {
        var entities = blocks.Select(b => new InterviewPracticeContentBlock
        {
            InterviewPracticeId = practice.Id,
            OrderIndex = b.OrderIndex,
            Type = b.Type,
            Content = b.Content,
            CodeContent = b.CodeContent,
            Language = b.Language,
            FileName = b.FileName,
            CalloutVariant = b.CalloutVariant,
            ImageUrl = b.ImageUrl,
            ImageAlt = b.ImageAlt,
            ImageCaption = b.ImageCaption,
            ImageWidth = b.ImageWidth,
        }).ToList();

        practice.ContentBlocks = entities;
    }

    internal static async Task<string> EnsureUniqueSlugAsync(string? requested, string title, Guid? currentId, AppDbContext dbContext, CancellationToken ct)
    {
        var baseSlug = Slugify(string.IsNullOrWhiteSpace(requested) ? title : requested);
        if (string.IsNullOrWhiteSpace(baseSlug))
            throw new AppException("Interview practice slug is invalid.");

        var slug = baseSlug;
        var suffix = 1;
        while (await dbContext.InterviewPractices.AnyAsync(x => x.Slug == slug && x.Id != currentId, ct))
            slug = $"{baseSlug}-{suffix++}";

        return slug;
    }

    private async Task<string> EnsureUniqueSlugAsync(string? requested, string title, Guid? currentId, CancellationToken ct)
        => await EnsureUniqueSlugAsync(requested, title, currentId, dbContext, ct);

    internal static string Slugify(string value)
    {
        var chars = value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        return slug.Trim('-');
    }

    private static void ValidateRequest(string title, string question)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new AppException("Title is required.");
        if (string.IsNullOrWhiteSpace(question))
            throw new AppException("Question is required.");
    }

    internal static InterviewPracticeDetailDto Map(InterviewPracticeEntity p) => new(
        p.Id, p.TopicId, p.Topic?.Slug ?? string.Empty,
        p.Title, p.Slug, p.Question,
        p.Level.ToString(), p.IsPublished, p.OrderIndex,
        p.CreatedAtUtc, p.UpdatedAtUtc,
        p.ContentBlocks
            .OrderBy(b => b.OrderIndex)
            .Select(b => new InterviewPracticeContentBlockDto(
                b.Id, b.OrderIndex, b.Type.ToString(), b.Content, b.CodeContent,
                b.Language, b.FileName, b.CalloutVariant?.ToString(),
                b.ImageUrl, b.ImageAlt, b.ImageCaption, b.ImageWidth?.ToString()))
            .ToList());
}
