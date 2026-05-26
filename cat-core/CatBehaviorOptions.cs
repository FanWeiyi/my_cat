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

    public TimeSpan DragLiftDuration { get; init; } = TimeSpan.FromMilliseconds(420);

    public TimeSpan DragHoldDuration { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan DragDropDuration { get; init; } = TimeSpan.FromSeconds(1.1);

    public TimeSpan MouseNoticeDuration { get; init; } = TimeSpan.FromSeconds(1.3);

    public TimeSpan MouseTrackDuration { get; init; } = TimeSpan.FromSeconds(3);

    public TimeSpan WindowLingerDuration { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan WindowStartleDuration { get; init; } = TimeSpan.FromMilliseconds(650);

    public TimeSpan WindowAvoidDuration { get; init; } = TimeSpan.FromSeconds(1.2);

    public TimeSpan TaskbarVisitDuration { get; init; } = TimeSpan.FromSeconds(8);

    public TimeSpan ObservationReactionDuration { get; init; } = TimeSpan.FromSeconds(2.2);
}
