using System.Net;
using Microsoft.EntityFrameworkCore;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Domain.Entities;
using skillexa_backend.Domain.Enums;
using skillexa_backend.Features.Auth;
using skillexa_backend.Infrastructure.Data;

namespace skillexa_backend.Features.Users;

public sealed class UserService(AppDbContext dbContext, IAuthService authService) : IUserService
{
    public async Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new UserSummaryDto(x.Id, x.Name, x.Email, x.Role.ToString(), x.Status.ToString(), x.AvatarUrl, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDetailDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        return Map(user);
    }

    public async Task<UserDetailDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new AppException("Name is required.");
        }

        var user = await FindUserAsync(userId, cancellationToken);
        user.Name = request.Name.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task<UserDetailDto> UpdateUserRoleAsync(Guid currentUserId, Guid targetUserId, UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(targetUserId, cancellationToken);

        if (currentUserId == targetUserId && request.Role != UserRole.Admin)
        {
            throw new AppException("Admin cannot demote themselves.", HttpStatusCode.BadRequest);
        }

        user.Role = request.Role;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task<UserDetailDto> UpdateUserStatusAsync(Guid currentUserId, Guid targetUserId, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(targetUserId, cancellationToken);

        if (currentUserId == targetUserId && request.Status != UserStatus.Active)
        {
            throw new AppException("Admin cannot deactivate themselves.", HttpStatusCode.BadRequest);
        }

        user.Status = request.Status;
        user.UpdatedAtUtc = DateTime.UtcNow;

        if (request.Status == UserStatus.Inactive)
        {
            await authService.RevokeAllRefreshTokensAsync(targetUserId, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    private async Task<User> FindUserAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AppException("User was not found.", HttpStatusCode.NotFound);

    private static UserDetailDto Map(User user)
        => new(user.Id, user.Name, user.Email, user.Role.ToString(), user.Status.ToString(), user.AvatarUrl, user.CreatedAtUtc, user.UpdatedAtUtc);
}
