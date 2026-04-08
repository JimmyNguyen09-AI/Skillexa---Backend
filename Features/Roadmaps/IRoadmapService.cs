namespace skillexa_backend.Features.Roadmaps;

public interface IRoadmapService
{
    Task<IReadOnlyList<RoadmapSummaryDto>> GetRoadmapsAsync(Guid userId, bool includeUnpublished, CancellationToken cancellationToken);
    Task<RoadmapDetailDto> GetRoadmapBySlugAsync(Guid userId, string slug, bool includeUnpublished, CancellationToken cancellationToken);
    Task<RoadmapDetailDto> CreateAsync(Guid userId, UpsertRoadmapRequest request, bool includeUnpublished, CancellationToken cancellationToken);
    Task<RoadmapDetailDto> UpdateAsync(Guid userId, Guid roadmapId, UpsertRoadmapRequest request, bool includeUnpublished, CancellationToken cancellationToken);
    Task DeleteAsync(Guid roadmapId, CancellationToken cancellationToken);
    Task<RoadmapDetailDto> AddCourseAsync(Guid userId, Guid roadmapId, UpsertRoadmapCourseRequest request, bool includeUnpublished, CancellationToken cancellationToken);
    Task<RoadmapDetailDto> RemoveCourseAsync(Guid userId, Guid roadmapId, Guid courseId, bool includeUnpublished, CancellationToken cancellationToken);
}
