namespace MyCat.CatCore;

public sealed record CatBehaviorOptions
{
    public TimeSpan IdleDuration { get; init; } = TimeSpan.FromSeconds(45);

    public TimeSpan RestDuration { get; init; } = TimeSpan.FromMinutes(3);

    public TimeSpan WalkDuration { get; init; } = TimeSpan.FromSeconds(10);

    public TimeSpan WakeDuration { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan EdgePauseDuration { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan PetReactionDuration { get; init; } = TimeSpan.FromSeconds(2.4);

    public TimeSpan DragSettleDuration { get; init; } = TimeSpan.FromSeconds(1.4);

    public TimeSpan MouseNoticeDuration { get; init; } = TimeSpan.FromSeconds(1.3);

    public TimeSpan WindowLingerDuration { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan ObservationReactionDuration { get; init; } = TimeSpan.FromSeconds(2.2);
}
