namespace skillexa_backend.Features.Roadmaps;

public interface IRoadmapService
{
    Task<IReadOnlyList<RoadmapSummaryDto>> GetRoadmapsAsync(Guid userId, bool includeUnpublished, CancellationToken cancellationToken);
    Task<RoadmapDetailDto> GetRoadmapBySlugAsync(Guid userId, string slug, bool includeUnpublished, CancellationToken cancellationToken);
}
