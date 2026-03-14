namespace skillexa_backend.Features.Stats;

public interface IStatsService
{
    Task<DashboardStatsDto> GetDashboardAsync(CancellationToken cancellationToken);
}
