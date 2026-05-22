namespace MyCat.CatCore;

public sealed class CatBehaviorController
{
    private readonly CatBehaviorOptions _options;
    private readonly Func<double> _sample;
    private CatState _resumeAfterPet = CatState.Idle;
    private CatState _resumeAfterReaction = CatState.Idle;
    private CatState _stateAfterWake = CatState.Idle;
    private CatActionId _observationActionId = CatActionId.ObservationAccompany;

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

        if (enabled && Current?.State is CatState.Walk or CatState.MouseNotice or CatState.WindowLinger)
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

    public CatActionTransition ReachWalkEdge(DateTimeOffset now)
    {
        return Current?.State is CatState.Walk
            ? SetState(CatState.EdgePause, now)
            : Current ?? Start(now);
    }

    public CatActionTransition DragSettled(DateTimeOffset now)
    {
        _resumeAfterReaction = CatState.Idle;
        return SetState(CatState.DragSettle, now);
    }

    public CatActionTransition? NoticeMouse(DateTimeOffset now)
    {
        if (QuietMode || Current?.State is not CatState.Idle)
        {
            return null;
        }

        _resumeAfterReaction = CatState.Idle;
        return SetState(CatState.MouseNotice, now);
    }

    public CatActionTransition? LingerByWindow(DateTimeOffset now)
    {
        if (QuietMode || Current?.State is not CatState.Idle and not CatState.EdgePause and not CatState.Walk)
        {
            return null;
        }

        _resumeAfterReaction = CatState.Idle;
        return SetState(CatState.WindowLinger, now);
    }

    public CatActionTransition ReactToObservation(CatEventType eventType, DateTimeOffset now)
    {
        _observationActionId = eventType switch
        {
            CatEventType.Rest => CatActionId.ObservationRest,
            CatEventType.Activity => CatActionId.ObservationActivity,
            CatEventType.Accompany => CatActionId.ObservationAccompany,
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null)
        };
        _resumeAfterReaction = eventType is CatEventType.Rest
            ? CatState.Rest
            : CatState.Idle;

        return SetState(CatState.ObservationReact, now);
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
            CatState.DragSettle => SetState(_resumeAfterReaction, now),
            CatState.MouseNotice => SetState(_resumeAfterReaction, now),
            CatState.WindowLinger => SetState(_resumeAfterReaction, now),
            CatState.ObservationReact => SetState(_resumeAfterReaction, now),
            CatState.Wake => SetState(_stateAfterWake, now),
            CatState.EdgePause => SetState(CatState.Idle, now),
            CatState.Walk => SetState(CatState.Idle, now),
            CatState.Rest => LeaveRest(now),
            _ => SetState(ChooseAutomaticState(now), now)
        };
    }

    private CatActionTransition LeaveRest(DateTimeOffset now)
    {
        var next = ChooseAutomaticState(now);
        if (next is CatState.Rest)
        {
            return SetState(next, now);
        }

        _stateAfterWake = next;
        return SetState(CatState.Wake, now);
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
            CatState.Wake => (CatActionId.WakeStretch, _options.WakeDuration),
            CatState.Walk => (CatActionId.WalkSlow, _options.WalkDuration),
            CatState.EdgePause => (CatActionId.EdgeStop, _options.EdgePauseDuration),
            CatState.PetReact => (CatActionId.PetReact, _options.PetReactionDuration),
            CatState.DragSettle => (CatActionId.DragSettle, _options.DragSettleDuration),
            CatState.MouseNotice => (CatActionId.MouseNotice, _options.MouseNoticeDuration),
            CatState.WindowLinger => (CatActionId.WindowLinger, _options.WindowLingerDuration),
            CatState.ObservationReact => (_observationActionId, _options.ObservationReactionDuration),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        Current = new CatActionTransition(state, actionId, now, now + duration);
        return Current;
    }
}
