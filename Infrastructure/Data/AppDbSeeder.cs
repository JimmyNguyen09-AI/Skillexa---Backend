using Microsoft.EntityFrameworkCore;
using skillexa_backend.Domain.Entities;
using skillexa_backend.Domain.Enums;

namespace skillexa_backend.Infrastructure.Data;

public static class AppDbSeeder
{
    public static async Task SeedAdminAsync(AppDbContext dbContext, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var options = configuration.GetSection("AdminSeed").Get<AdminSeedOptions>();
        if (options is null || !options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Email) ||
            string.IsNullOrWhiteSpace(options.Password) ||
            string.IsNullOrWhiteSpace(options.Name))
        {
            throw new InvalidOperationException("AdminSeed is enabled but Name, Email, or Password is missing.");
        }

        var email = options.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Name = options.Name.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(options.Password),
                Role = UserRole.Admin,
                Status = UserStatus.Active
            };

            dbContext.Users.Add(user);
        }
        else
        {
            user.Name = options.Name.Trim();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(options.Password);
            user.Role = UserRole.Admin;
            user.Status = UserStatus.Active;
            user.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
