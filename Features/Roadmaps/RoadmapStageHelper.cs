namespace skillexa_backend.Features.Roadmaps;

public static class RoadmapStageHelper
{
    public static string GetStage(int progressPercent)
    {
        return progressPercent switch
        {
            < 20 => "Seed",
            < 40 => "Sprout",
            < 60 => "Young Tree",
            < 80 => "Mature Tree",
            _ => "Ancient Tree"
        };
    }
}
