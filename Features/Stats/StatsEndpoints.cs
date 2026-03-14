using skillexa_backend.Common.Results;

namespace skillexa_backend.Features.Stats;

public static class StatsEndpoints
{
    public static IEndpointRouteBuilder MapStatsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/stats/dashboard", async (IStatsService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetDashboardAsync(cancellationToken);
            return Results.Ok(ApiResponse<DashboardStatsDto>.Ok(result));
        })
        .WithTags("Stats")
        .RequireAuthorization("AdminOnly");

        return app;
    }
}
