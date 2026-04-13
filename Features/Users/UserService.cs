using System.Net;
using Microsoft.EntityFrameworkCore;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Common.Subscriptions;
using skillexa_backend.Domain.Entities;
using skillexa_backend.Domain.Enums;
using skillexa_backend.Features.Auth;
using skillexa_backend.Infrastructure.Data;

namespace skillexa_backend.Features.Users;

public sealed class UserService(AppDbContext dbContext, IAuthService authService) : IUserService
{
    public async Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var today = GetUtcToday();
        return users.Select(user => MapSummary(user, today)).ToList();
    }

    public async Task<UserDetailDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        return Map(user);
    }

    public async Task<UserDetailDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new AppException("Name is required.");
        }

        var user = await FindUserAsync(userId, cancellationToken);
        user.Name = request.Name.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task<UserDetailDto> UpdateUserRoleAsync(Guid currentUserId, Guid targetUserId, UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(targetUserId, cancellationToken);

        if (currentUserId == targetUserId && request.Role != UserRole.Admin)
        {
            throw new AppException("Admin cannot demote themselves.", HttpStatusCode.BadRequest);
        }

        user.Role = request.Role;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task<UserDetailDto> UpdateUserStatusAsync(Guid currentUserId, Guid targetUserId, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(targetUserId, cancellationToken);

        if (currentUserId == targetUserId && request.Status != UserStatus.Active)
        {
            throw new AppException("Admin cannot deactivate themselves.", HttpStatusCode.BadRequest);
        }

        user.Status = request.Status;
        user.UpdatedAtUtc = DateTime.UtcNow;

        if (request.Status == UserStatus.Inactive)
        {
            await authService.RevokeAllRefreshTokensAsync(targetUserId, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task<UserDetailDto> UpdateUserPlanAsync(Guid targetUserId, UpdateUserPlanRequest request, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(targetUserId, cancellationToken);
        user.MembershipPlan = request.MembershipPlan;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    private async Task<User> FindUserAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AppException("User was not found.", HttpStatusCode.NotFound);

    private static UserSummaryDto MapSummary(User user, DateOnly today)
    {
        var (usageCount, usageRemaining) = GetDailyAiUsage(user, today);
        return new UserSummaryDto(
            user.Id,
            user.Name,
            user.Email,
            user.Role.ToString(),
            user.Status.ToString(),
            user.MembershipPlan.ToString(),
            usageCount,
            usageRemaining,
            user.AvatarUrl,
            user.CreatedAtUtc);
    }

    private static UserDetailDto Map(User user)
    {
        var (usageCount, usageRemaining) = GetDailyAiUsage(user, GetUtcToday());
        return new UserDetailDto(
            user.Id,
            user.Name,
            user.Email,
            user.Role.ToString(),
            user.Status.ToString(),
            user.MembershipPlan.ToString(),
            usageCount,
            usageRemaining,
            user.AvatarUrl,
            user.CreatedAtUtc,
            user.UpdatedAtUtc);
    }

    private static (int UsageCount, int UsageRemaining) GetDailyAiUsage(User user, DateOnly today)
    {
        var hasUnlimitedUsage = user.Role == UserRole.Admin || user.MembershipPlan == MembershipPlan.Pro;
        if (hasUnlimitedUsage)
        {
            return (user.AiAgentUsageCount, int.MaxValue);
        }

        var usageCount = user.AiAgentUsageDateUtc == today ? user.AiAgentUsageCount : 0;
        var usageRemaining = Math.Max(SubscriptionLimits.FreeAiAgentUsageLimit - usageCount, 0);
        return (usageCount, usageRemaining);
    }

    private static DateOnly GetUtcToday() => DateOnly.FromDateTime(DateTime.UtcNow);
}
