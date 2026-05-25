namespace MyCat.CatCore;

public sealed record CatLearningState(IReadOnlyList<string> SeenFeedbackKeys)
{
    public static readonly CatLearningState Empty = new(Array.Empty<string>());
}

