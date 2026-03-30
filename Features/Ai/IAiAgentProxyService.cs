namespace skillexa_backend.Features.Ai;

public interface IAiAgentProxyService
{
    Task<(string Body, AiAgentUsageDto Usage)> ProxyReplyAsync(Guid userId, string requestBody, string? authorizationHeader, CancellationToken cancellationToken);
    Task<(HttpResponseMessage UpstreamResponse, AiAgentUsageDto Usage)> ProxyStreamAsync(Guid userId, string requestBody, string? authorizationHeader, CancellationToken cancellationToken);
    Task ProxyResetAsync(string requestBody, string? authorizationHeader, CancellationToken cancellationToken);
    Task<AiAgentUsageDto> GetUsageAsync(Guid userId, CancellationToken cancellationToken);
}
