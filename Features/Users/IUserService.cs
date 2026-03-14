namespace skillexa_backend.Features.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken);
    Task<UserDetailDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserDetailDto> UpdateUserRoleAsync(Guid currentUserId, Guid targetUserId, UpdateUserRoleRequest request, CancellationToken cancellationToken);
    Task<UserDetailDto> UpdateUserStatusAsync(Guid currentUserId, Guid targetUserId, UpdateUserStatusRequest request, CancellationToken cancellationToken);
}
