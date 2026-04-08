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

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Name))
        {
            throw new InvalidOperationException("AdminSeed is enabled but Name or Email is missing.");
        }

        var email = options.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Name = options.Name.Trim(),
                Email = email,
                PasswordHash = Guid.NewGuid().ToString("N"),
                Role = UserRole.Admin,
                Status = UserStatus.Active
            };

            dbContext.Users.Add(user);
        }
        else
        {
            user.Name = options.Name.Trim();
            user.Role = UserRole.Admin;
            user.Status = UserStatus.Active;
            user.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static async Task SeedRoadmapsAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Roadmaps.AnyAsync(cancellationToken))
        {
            return;
        }

        var definitions = RoadmapSeedDefaults.All;
        var createdRoadmaps = new List<Roadmap>();

        foreach (var definition in definitions)
        {
            var roadmap = new Roadmap
            {
                Name = definition.Name,
                Slug = definition.Slug,
                Description = definition.Description
            };

            dbContext.Roadmaps.Add(roadmap);
            createdRoadmaps.Add(roadmap);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var coursesBySlug = await dbContext.Courses
            .ToDictionaryAsync(x => x.Slug.ToLowerInvariant(), cancellationToken);

        foreach (var definition in definitions)
        {
            var roadmap = createdRoadmaps.First(x => x.Slug == definition.Slug);
            var desiredCourseIds = definition.CourseDefinitions
                .Select(definitionCourse => new
                {
                    definitionCourse.OrderIndex,
                    Course = definitionCourse.CourseSlugAliases
                        .Select(alias => coursesBySlug.GetValueOrDefault(alias.Trim().ToLowerInvariant()))
                        .FirstOrDefault(x => x is not null)
                })
                .Where(x => x.Course is not null)
                .Select(x => new { x.OrderIndex, CourseId = x.Course!.Id })
                .ToList();

            foreach (var desired in desiredCourseIds)
            {
                dbContext.RoadmapCourses.Add(new RoadmapCourse
                {
                    RoadmapId = roadmap.Id,
                    CourseId = desired.CourseId,
                    OrderIndex = desired.OrderIndex
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
