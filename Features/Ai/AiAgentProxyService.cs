using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Common.Subscriptions;
using skillexa_backend.Domain.Entities;
using skillexa_backend.Domain.Enums;
using skillexa_backend.Infrastructure.Data;

namespace skillexa_backend.Features.Ai;

public sealed class AiAgentProxyService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    AppDbContext dbContext) : IAiAgentProxyService
{
    private const string ClientName = "AiAgentProxy";

    public async Task<(string Body, AiAgentUsageDto Usage)> ProxyReplyAsync(
        Guid userId,
        string requestBody,
        string? authorizationHeader,
        CancellationToken cancellationToken)
    {
        var user = await GetAndValidateUserAsync(userId, cancellationToken);
        using var request = BuildRequest(HttpMethod.Post, "lesson-agent/reply", requestBody, authorizationHeader);
        var client = httpClientFactory.CreateClient(ClientName);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw CreateUpstreamException(body, response.StatusCode);
            }

            var usage = await IncrementUsageAsync(user, cancellationToken);
            return (body, usage);
        }
        catch (HttpRequestException)
        {
            throw new AppException("Could not reach the AI assistant service.", HttpStatusCode.BadGateway);
        }
    }

    public async Task<(HttpResponseMessage UpstreamResponse, AiAgentUsageDto Usage)> ProxyStreamAsync(
        Guid userId,
        string requestBody,
        string? authorizationHeader,
        CancellationToken cancellationToken)
    {
        var user = await GetAndValidateUserAsync(userId, cancellationToken);
        var request = BuildRequest(HttpMethod.Post, "lesson-agent/stream", requestBody, authorizationHeader);
        var client = httpClientFactory.CreateClient(ClientName);

        try
        {
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                response.Dispose();
                request.Dispose();
                throw CreateUpstreamException(body, response.StatusCode);
            }

            request.Dispose();
            var usage = await IncrementUsageAsync(user, cancellationToken);
            return (response, usage);
        }
        catch (HttpRequestException)
        {
            request.Dispose();
            throw new AppException("Could not reach the AI assistant service.", HttpStatusCode.BadGateway);
        }
    }

    public async Task ProxyResetAsync(string requestBody, string? authorizationHeader, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Post, "lesson-agent/reset", requestBody, authorizationHeader);
        var client = httpClientFactory.CreateClient(ClientName);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw CreateUpstreamException(body, response.StatusCode);
            }
        }
        catch (HttpRequestException)
        {
            throw new AppException("Could not reach the AI assistant service.", HttpStatusCode.BadGateway);
        }
    }

    public async Task<AiAgentUsageDto> GetUsageAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AppException("User was not found.", HttpStatusCode.NotFound);

        var today = GetUtcToday();
        var didReset = ResetUsageWindowIfNeeded(user, today);
        if (didReset)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapUsage(user, today);
    }

    private async Task<User> GetAndValidateUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AppException("User was not found.", HttpStatusCode.NotFound);

        if (user.Status != UserStatus.Active)
        {
            throw new AppException("Your account is inactive.", HttpStatusCode.Forbidden);
        }

        var today = GetUtcToday();
        ResetUsageWindowIfNeeded(user, today);

        if (user.Role != UserRole.Admin &&
            user.MembershipPlan == MembershipPlan.Free &&
            user.AiAgentUsageCount >= SubscriptionLimits.FreeAiAgentUsageLimit)
        {
            throw new AppException("You have used all 5 free AI requests for today. Upgrade to Pro to continue immediately, or come back tomorrow.", HttpStatusCode.Forbidden);
        }

        return user;
    }

    private async Task<AiAgentUsageDto> IncrementUsageAsync(User user, CancellationToken cancellationToken)
    {
        var today = GetUtcToday();
        ResetUsageWindowIfNeeded(user, today);

        if (user.Role != UserRole.Admin && user.MembershipPlan == MembershipPlan.Free)
        {
            user.AiAgentUsageCount += 1;
            user.AiAgentUsageDateUtc = today;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapUsage(user, today);
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string relativePath, string requestBody, string? authorizationHeader)
    {
        var baseUrl = configuration["AiAgent:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new AppException("AI agent is not configured on this environment.", HttpStatusCode.ServiceUnavailable);
        }

        var request = new HttpRequestMessage(method, $"{baseUrl.TrimEnd('/')}/{relativePath}");
        request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        if (!string.IsNullOrWhiteSpace(authorizationHeader) && AuthenticationHeaderValue.TryParse(authorizationHeader, out var authValue))
        {
            request.Headers.Authorization = authValue;
        }

        var internalApiKey = configuration["AiAgent:InternalApiKey"];
        if (!string.IsNullOrWhiteSpace(internalApiKey))
        {
            request.Headers.TryAddWithoutValidation("X-Internal-Api-Key", internalApiKey);
        }

        return request;
    }

    private static AiAgentUsageDto MapUsage(User user, DateOnly? today = null)
    {
        var hasUnlimitedUsage = user.Role == UserRole.Admin || user.MembershipPlan == MembershipPlan.Pro;
        var usageLimit = hasUnlimitedUsage ? int.MaxValue : SubscriptionLimits.FreeAiAgentUsageLimit;
        var effectiveUsageCount = GetEffectiveUsageCount(user, today ?? GetUtcToday());
        var usageRemaining = hasUnlimitedUsage
            ? int.MaxValue
            : Math.Max(SubscriptionLimits.FreeAiAgentUsageLimit - effectiveUsageCount, 0);

        return new AiAgentUsageDto(user.MembershipPlan.ToString(), effectiveUsageCount, usageRemaining, usageLimit);
    }

    private static bool ResetUsageWindowIfNeeded(User user, DateOnly today)
    {
        if (user.Role == UserRole.Admin || user.MembershipPlan == MembershipPlan.Pro || user.AiAgentUsageDateUtc == today)
        {
            return false;
        }

        user.AiAgentUsageCount = 0;
        user.AiAgentUsageDateUtc = today;
        user.UpdatedAtUtc = DateTime.UtcNow;
        return true;
    }

    private static int GetEffectiveUsageCount(User user, DateOnly today)
        => user.Role == UserRole.Admin || user.MembershipPlan == MembershipPlan.Pro || user.AiAgentUsageDateUtc == today
            ? user.AiAgentUsageCount
            : 0;

    private static DateOnly GetUtcToday() => DateOnly.FromDateTime(DateTime.UtcNow);

    private static AppException CreateUpstreamException(string? responseBody, HttpStatusCode statusCode)
    {
        var message = string.IsNullOrWhiteSpace(responseBody)
            ? "AI assistant is unavailable right now."
            : responseBody;

        return new AppException(message, statusCode == HttpStatusCode.TooManyRequests ? HttpStatusCode.TooManyRequests : HttpStatusCode.BadGateway);
    }
}
