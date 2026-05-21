namespace MyCat.CatCore;

public sealed class CatBehaviorController
{
    private readonly CatBehaviorOptions _options;
    private readonly Func<double> _sample;
    private CatState _resumeAfterPet = CatState.Idle;

    public CatBehaviorController(CatBehaviorOptions? options = null, Func<double>? sample = null)
    {
        _options = options ?? new CatBehaviorOptions();
        _sample = sample ?? new Random().NextDouble;
    }

    public CatActionTransition? Current { get; private set; }

    public CatHabitProfile HabitProfile { get; set; } = CatHabitProfile.FromEvents(Array.Empty<CatObservationEvent>());

    public bool QuietMode { get; private set; }

    public CatActionTransition Start(DateTimeOffset now)
    {
        return SetState(CatState.Idle, now);
    }

    public CatActionTransition SetQuietMode(bool enabled, DateTimeOffset now)
    {
        QuietMode = enabled;

        if (enabled && Current?.State is CatState.Walk)
        {
            return SetState(CatState.Idle, now);
        }

        return Current ?? Start(now);
    }

    public CatActionTransition Pet(DateTimeOffset now)
    {
        _resumeAfterPet = Current?.State is CatState.Rest
            ? CatState.Rest
            : CatState.Idle;

        return SetState(CatState.PetReact, now);
    }

    public CatActionTransition? Advance(DateTimeOffset now)
    {
        if (Current is null)
        {
            return Start(now);
        }

        if (now < Current.EndsAt)
        {
            return null;
        }

        return Current.State switch
        {
            CatState.PetReact => SetState(_resumeAfterPet, now),
            CatState.Walk => SetState(CatState.Idle, now),
            _ => SetState(ChooseAutomaticState(now), now)
        };
    }

    private CatState ChooseAutomaticState(DateTimeOffset now)
    {
        var weights = HabitProfile.For(CatTimeBucketResolver.Resolve(now));
        var activityWeight = QuietMode
            ? weights.ActivityWeight * 0.08
            : weights.ActivityWeight;
        var total = weights.RestWeight + activityWeight + weights.AccompanyWeight;
        var restEdge = weights.RestWeight / total;
        var activityEdge = restEdge + (activityWeight / total);
        var sample = _sample();

        if (sample < restEdge)
        {
            return CatState.Rest;
        }

        if (sample < activityEdge)
        {
            return CatState.Walk;
        }

        return CatState.Idle;
    }

    private CatActionTransition SetState(CatState state, DateTimeOffset now)
    {
        var (actionId, duration) = state switch
        {
            CatState.Idle => (CatActionId.IdleSit, _options.IdleDuration),
            CatState.Rest => (CatActionId.RestSleep, _options.RestDuration),
            CatState.Walk => (CatActionId.WalkSlow, _options.WalkDuration),
            CatState.PetReact => (CatActionId.PetReact, _options.PetReactionDuration),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        Current = new CatActionTransition(state, actionId, now, now + duration);
        return Current;
    }
}
