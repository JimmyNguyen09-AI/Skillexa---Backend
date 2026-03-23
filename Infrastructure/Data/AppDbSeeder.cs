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
        var definitions = RoadmapSeedDefaults.All;
        var existingRoadmaps = await dbContext.Roadmaps
            .Include(x => x.RoadmapCourses)
            .ToListAsync(cancellationToken);

        foreach (var definition in definitions)
        {
            var roadmap = existingRoadmaps.FirstOrDefault(x => x.Slug == definition.Slug);
            if (roadmap is null)
            {
                roadmap = new Roadmap
                {
                    Name = definition.Name,
                    Slug = definition.Slug,
                    Description = definition.Description
                };

                dbContext.Roadmaps.Add(roadmap);
                existingRoadmaps.Add(roadmap);
            }
            else
            {
                roadmap.Name = definition.Name;
                roadmap.Description = definition.Description;
                roadmap.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var coursesBySlug = await dbContext.Courses
            .ToDictionaryAsync(x => x.Slug.ToLowerInvariant(), cancellationToken);

        foreach (var definition in definitions)
        {
            var roadmap = existingRoadmaps.First(x => x.Slug == definition.Slug);
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

            var desiredCourseIdSet = desiredCourseIds.Select(x => x.CourseId).ToHashSet();
            var existingMappings = await dbContext.RoadmapCourses
                .Where(x => x.RoadmapId == roadmap.Id)
                .ToListAsync(cancellationToken);

            foreach (var staleMapping in existingMappings.Where(x => !desiredCourseIdSet.Contains(x.CourseId)))
            {
                dbContext.RoadmapCourses.Remove(staleMapping);
            }

            foreach (var desired in desiredCourseIds)
            {
                var mapping = existingMappings.FirstOrDefault(x => x.CourseId == desired.CourseId);
                if (mapping is null)
                {
                    dbContext.RoadmapCourses.Add(new RoadmapCourse
                    {
                        RoadmapId = roadmap.Id,
                        CourseId = desired.CourseId,
                        OrderIndex = desired.OrderIndex
                    });
                }
                else if (mapping.OrderIndex != desired.OrderIndex)
                {
                    mapping.OrderIndex = desired.OrderIndex;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
