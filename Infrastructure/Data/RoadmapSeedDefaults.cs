namespace skillexa_backend.Infrastructure.Data;

public static class RoadmapSeedDefaults
{
    public static IReadOnlyList<RoadmapSeedDefinition> All { get; } =
    [
        new(
            "Frontend Developer",
            "frontend-developer",
            "Grow from markup and styling to component-driven interfaces, type-safe apps, and modern frontend delivery.",
            [
                new RoadmapCourseSeedDefinition(1, ["front-end-fundamentals-html-css", "html-css-fundamentals", "html-css", "html", "css", "frontend-fundamentals"]),
                new RoadmapCourseSeedDefinition(2, ["javascript-fundamentals", "javascript", "js-fundamentals"]),
                new RoadmapCourseSeedDefinition(3, ["react-js-from-beginner-to-advanced-typescript", "react-typescript", "react-fundamentals", "react"]),
                new RoadmapCourseSeedDefinition(4, ["tailwind-css", "tailwind", "tailwind-fundamentals"]),
                new RoadmapCourseSeedDefinition(5, ["typescript-fundamentals", "typescript", "typescript-for-beginners"]),
                new RoadmapCourseSeedDefinition(6, ["next-js-fundamentals", "next-js", "nextjs"])
            ]),
        new(
            "Backend Developer",
            "backend-developer",
            "Build a strong backend foundation with programming, data, APIs, authentication, and systems thinking.",
            [
                new RoadmapCourseSeedDefinition(1, ["programming-fundamentals", "python-for-beginner", "python-for-beginners", "backend-programming-fundamentals"]),
                new RoadmapCourseSeedDefinition(2, ["databases", "database-fundamentals", "sql-database-fundamentals"]),
                new RoadmapCourseSeedDefinition(3, ["api-development", "rest-api-development", "backend-api-development"]),
                new RoadmapCourseSeedDefinition(4, ["authentication", "auth-fundamentals", "jwt-authentication"]),
                new RoadmapCourseSeedDefinition(5, ["backend-framework", "asp-net-core", "nodejs-backend", "backend-framework-fundamentals"]),
                new RoadmapCourseSeedDefinition(6, ["system-design-basics", "system-design", "backend-system-design-basics"])
            ]),
        new(
            "DevOps Engineer",
            "devops-engineer",
            "Learn the tooling, automation, and deployment habits that help teams ship confidently.",
            [
                new RoadmapCourseSeedDefinition(1, ["linux-basics", "linux"]),
                new RoadmapCourseSeedDefinition(2, ["git-github", "git-and-github", "git-github-fundamentals"]),
                new RoadmapCourseSeedDefinition(3, ["docker", "docker-fundamentals"]),
                new RoadmapCourseSeedDefinition(4, ["ci-cd", "cicd", "ci-cd-fundamentals"]),
                new RoadmapCourseSeedDefinition(5, ["cloud-basics", "cloud-fundamentals", "deployment-basics"]),
                new RoadmapCourseSeedDefinition(6, ["monitoring-deployment", "monitoring", "deployment-monitoring"])
            ])
    ];
}

// Update the slug aliases below to match the real production course slugs in your catalog.
public sealed record RoadmapSeedDefinition(
    string Name,
    string Slug,
    string Description,
    IReadOnlyList<RoadmapCourseSeedDefinition> CourseDefinitions);

public sealed record RoadmapCourseSeedDefinition(
    int OrderIndex,
    IReadOnlyList<string> CourseSlugAliases);
