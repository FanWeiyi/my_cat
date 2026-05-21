namespace MyCat.CatCore;

public sealed record CatBehaviorOptions
{
    public TimeSpan IdleDuration { get; init; } = TimeSpan.FromSeconds(8);

    public TimeSpan RestDuration { get; init; } = TimeSpan.FromSeconds(12);

    public TimeSpan WalkDuration { get; init; } = TimeSpan.FromSeconds(6);

    public TimeSpan PetReactionDuration { get; init; } = TimeSpan.FromSeconds(2);
}

