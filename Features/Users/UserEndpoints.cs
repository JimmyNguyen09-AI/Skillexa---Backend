using skillexa_backend.Common.Results;
using skillexa_backend.Infrastructure.Security;

namespace skillexa_backend.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapGet("/me", async (HttpContext httpContext, IUserService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetCurrentUserAsync(httpContext.User.GetRequiredUserId(), cancellationToken);
            return Results.Ok(ApiResponse<UserDetailDto>.Ok(result));
        }).RequireAuthorization();

        group.MapGet("/", async (IUserService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetUsersAsync(cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyList<UserSummaryDto>>.Ok(result));
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/{userId:guid}/role", async (Guid userId, UpdateUserRoleRequest request, HttpContext httpContext, IUserService service, CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateUserRoleAsync(httpContext.User.GetRequiredUserId(), userId, request, cancellationToken);
            return Results.Ok(ApiResponse<UserDetailDto>.Ok(result, "User role updated."));
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/{userId:guid}/status", async (Guid userId, UpdateUserStatusRequest request, HttpContext httpContext, IUserService service, CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateUserStatusAsync(httpContext.User.GetRequiredUserId(), userId, request, cancellationToken);
            return Results.Ok(ApiResponse<UserDetailDto>.Ok(result, "User status updated."));
        }).RequireAuthorization("AdminOnly");

        return app;
    }
}
