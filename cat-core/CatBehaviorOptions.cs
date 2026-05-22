namespace MyCat.CatCore;

public sealed record CatBehaviorOptions
{
    public TimeSpan IdleDuration { get; init; } = TimeSpan.FromSeconds(8);

    public TimeSpan RestDuration { get; init; } = TimeSpan.FromSeconds(12);

    public TimeSpan WalkDuration { get; init; } = TimeSpan.FromSeconds(6);

    public TimeSpan WakeDuration { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan EdgePauseDuration { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan PetReactionDuration { get; init; } = TimeSpan.FromSeconds(2);
}
