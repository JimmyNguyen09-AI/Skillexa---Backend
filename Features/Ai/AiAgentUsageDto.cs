namespace skillexa_backend.Features.Ai;

public sealed record AiAgentUsageDto(
    string MembershipPlan,
    int UsageCount,
    int UsageRemaining,
    int UsageLimit);
