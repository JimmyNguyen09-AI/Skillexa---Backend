using System.Text;
using skillexa_backend.Common.Results;
using skillexa_backend.Infrastructure.Security;

namespace skillexa_backend.Features.Ai;

public static class AiAgentEndpoints
{
    public static IEndpointRouteBuilder MapAiAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai/lesson-agent")
            .WithTags("AI")
            .RequireAuthorization();

        group.MapGet("/usage", async (HttpContext httpContext, IAiAgentProxyService service, CancellationToken cancellationToken) =>
        {
            var usage = await service.GetUsageAsync(httpContext.User.GetRequiredUserId(), cancellationToken);
            return Results.Ok(ApiResponse<AiAgentUsageDto>.Ok(usage));
        });

        group.MapPost("/reply", async (HttpContext httpContext, IAiAgentProxyService service, CancellationToken cancellationToken) =>
        {
            var requestBody = await ReadRequestBodyAsync(httpContext.Request, cancellationToken);
            var (body, usage) = await service.ProxyReplyAsync(
                httpContext.User.GetRequiredUserId(),
                requestBody,
                httpContext.Request.Headers.Authorization.ToString(),
                cancellationToken);

            AppendUsageHeaders(httpContext.Response, usage);
            return Results.Content(body, "application/json", Encoding.UTF8);
        });

        group.MapPost("/stream", async (HttpContext httpContext, IAiAgentProxyService service, CancellationToken cancellationToken) =>
        {
            var requestBody = await ReadRequestBodyAsync(httpContext.Request, cancellationToken);
            var (upstreamResponse, usage) = await service.ProxyStreamAsync(
                httpContext.User.GetRequiredUserId(),
                requestBody,
                httpContext.Request.Headers.Authorization.ToString(),
                cancellationToken);

            using (upstreamResponse)
            {
                httpContext.Response.StatusCode = (int)upstreamResponse.StatusCode;
                httpContext.Response.ContentType = upstreamResponse.Content.Headers.ContentType?.ToString() ?? "text/event-stream";
                AppendUsageHeaders(httpContext.Response, usage);

                await using var upstreamStream = await upstreamResponse.Content.ReadAsStreamAsync(cancellationToken);
                await upstreamStream.CopyToAsync(httpContext.Response.Body, cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
            }
        });

        group.MapPost("/reset", async (HttpContext httpContext, IAiAgentProxyService service, CancellationToken cancellationToken) =>
        {
            var requestBody = await ReadRequestBodyAsync(httpContext.Request, cancellationToken);
            await service.ProxyResetAsync(requestBody, httpContext.Request.Headers.Authorization.ToString(), cancellationToken);
            return Results.Ok(ApiResponse<string>.Ok("ok", "AI session reset."));
        });

        return app;
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static void AppendUsageHeaders(HttpResponse response, AiAgentUsageDto usage)
    {
        response.Headers["X-AI-Plan"] = usage.MembershipPlan;
        response.Headers["X-AI-Usage-Count"] = usage.UsageCount.ToString();
        response.Headers["X-AI-Usage-Remaining"] = usage.UsageRemaining == int.MaxValue ? "unlimited" : usage.UsageRemaining.ToString();
        response.Headers["X-AI-Usage-Limit"] = usage.UsageLimit == int.MaxValue ? "unlimited" : usage.UsageLimit.ToString();
    }
}
