using skillexa_backend.Domain.Enums;

namespace skillexa_backend.Features.Users;

public sealed record UserSummaryDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    string Status,
    string MembershipPlan,
    int AiAgentUsageCount,
    int AiAgentUsageRemaining,
    string? AvatarUrl,
    DateTime CreatedAtUtc);

public sealed record UserDetailDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    string Status,
    string MembershipPlan,
    int AiAgentUsageCount,
    int AiAgentUsageRemaining,
    string? AvatarUrl,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record UpdateUserRoleRequest(UserRole Role);

public sealed record UpdateUserStatusRequest(UserStatus Status);

public sealed record UpdateUserPlanRequest(MembershipPlan MembershipPlan);

public sealed record UpdateProfileRequest(string Name);
