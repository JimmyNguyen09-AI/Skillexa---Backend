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
    public async Task<IReadOnlyList<InterviewPracticeSummaryDto>> GetAllAsync(Guid userId, CancellationToken ct)
    {
        await EnsureProAccessAsync(userId, ct);

        var isAdmin = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => x.Role == UserRole.Admin)
            .FirstOrDefaultAsync(ct);

        return await dbContext.InterviewPractices
            .AsQueryable()
            .Where(x => isAdmin || x.IsPublished)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new InterviewPracticeSummaryDto(
                x.Id, x.Title, x.Slug, x.Question, x.Description,
                x.Level.ToString(), x.Categories, x.ThumbnailUrl,
                x.IsPublished, x.CreatedAtUtc, x.UpdatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<InterviewPracticeDetailDto> GetBySlugAsync(string slug, Guid userId, CancellationToken ct)
    {
        await EnsureProAccessAsync(userId, ct);

        var isAdmin = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => x.Role == UserRole.Admin)
            .FirstOrDefaultAsync(ct);

        var practice = await dbContext.InterviewPractices
            .Include(x => x.ContentBlocks)
            .FirstOrDefaultAsync(x => x.Slug == slug, ct)
            ?? throw new AppException("Interview practice not found.", HttpStatusCode.NotFound);

        if (!isAdmin && !practice.IsPublished)
            throw new AppException("Interview practice not found.", HttpStatusCode.NotFound);

        return Map(practice);
    }

    public async Task<InterviewPracticeDetailDto> CreateAsync(CreateInterviewPracticeRequest request, CancellationToken ct)
    {
        ValidateRequest(request.Title, request.Question);
        var slug = await EnsureUniqueSlugAsync(request.Slug, request.Title, null, ct);

        var practice = new InterviewPracticeEntity
        {
            Title = request.Title.Trim(),
            Slug = slug,
            Question = request.Question.Trim(),
            Description = Normalize(request.Description),
            Level = request.Level,
            Categories = NormalizeCategories(request.Categories),
            ThumbnailUrl = Normalize(request.ThumbnailUrl),
            IsPublished = request.IsPublished,
        };

        dbContext.InterviewPractices.Add(practice);
        await dbContext.SaveChangesAsync(ct);

        UpsertBlocks(practice, request.ContentBlocks);
        await dbContext.SaveChangesAsync(ct);

        return Map(practice);
    }

    public async Task<InterviewPracticeDetailDto> UpdateAsync(Guid id, UpdateInterviewPracticeRequest request, CancellationToken ct)
    {
        ValidateRequest(request.Title, request.Question);

        var practice = await dbContext.InterviewPractices
            .Include(x => x.ContentBlocks)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Interview practice not found.", HttpStatusCode.NotFound);

        practice.Title = request.Title.Trim();
        practice.Slug = await EnsureUniqueSlugAsync(request.Slug, request.Title, practice.Id, ct);
        practice.Question = request.Question.Trim();
        practice.Description = Normalize(request.Description);
        practice.Level = request.Level;
        practice.Categories = NormalizeCategories(request.Categories);
        practice.ThumbnailUrl = Normalize(request.ThumbnailUrl);
        practice.IsPublished = request.IsPublished;
        practice.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.InterviewPracticeContentBlocks.RemoveRange(practice.ContentBlocks);
        UpsertBlocks(practice, request.ContentBlocks);

        await dbContext.SaveChangesAsync(ct);
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

    private async Task EnsureProAccessAsync(Guid userId, CancellationToken ct)
    {
        var user = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => new { x.Role, x.MembershipPlan })
            .FirstOrDefaultAsync(ct)
            ?? throw new AppException("User was not found.", HttpStatusCode.NotFound);

        if (user.Role != UserRole.Admin && user.MembershipPlan != MembershipPlan.Pro)
            throw new AppException("Interview Practice is available for Pro members only.", HttpStatusCode.Forbidden);
    }

    private void UpsertBlocks(InterviewPracticeEntity practice, IReadOnlyList<InterviewPracticeContentBlockRequest> blocks)
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

        dbContext.InterviewPracticeContentBlocks.AddRange(entities);
        practice.ContentBlocks = entities;
    }

    private async Task<string> EnsureUniqueSlugAsync(string? requested, string title, Guid? currentId, CancellationToken ct)
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

    private static string Slugify(string value)
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

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string[] NormalizeCategories(IReadOnlyCollection<CourseCategory>? categories)
    {
        if (categories is null || categories.Count == 0)
            return [nameof(CourseCategory.Fundamentals)];

        var normalized = categories.Distinct().Select(c => c.ToString()).ToArray();
        return normalized.Length == 0 ? [nameof(CourseCategory.Fundamentals)] : normalized;
    }

    private static InterviewPracticeDetailDto Map(InterviewPracticeEntity p) => new(
        p.Id, p.Title, p.Slug, p.Question, p.Description,
        p.Level.ToString(), p.Categories, p.ThumbnailUrl,
        p.IsPublished, p.CreatedAtUtc, p.UpdatedAtUtc,
        p.ContentBlocks
            .OrderBy(b => b.OrderIndex)
            .Select(b => new InterviewPracticeContentBlockDto(
                b.Id, b.OrderIndex, b.Type.ToString(), b.Content, b.CodeContent,
                b.Language, b.FileName, b.CalloutVariant?.ToString(),
                b.ImageUrl, b.ImageAlt, b.ImageCaption, b.ImageWidth?.ToString()))
            .ToList());
}
