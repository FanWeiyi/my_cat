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

    public CatBehaviorSettings BehaviorSettings { get; set; } = CatBehaviorSettings.Empty;

    public bool QuietMode { get; private set; }

    public CatActionTransition Start(DateTimeOffset now)
    {
        return SetState(CatState.Idle, now);
    }

    public CatActionTransition SetQuietMode(bool enabled, DateTimeOffset now)
    {
        QuietMode = enabled;

        if (enabled && Current?.State is CatState.Walk or CatState.MouseNotice or CatState.WindowLinger or CatState.TaskbarVisit)
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

    public CatActionTransition DragLifted(DateTimeOffset now)
    {
        _resumeAfterReaction = CatState.Idle;
        return SetState(CatState.DragLift, now);
    }

    public CatActionTransition DragHeld(DateTimeOffset now)
    {
        _resumeAfterReaction = CatState.Idle;
        return SetState(CatState.DragHold, now);
    }

    public CatActionTransition DragDropped(DateTimeOffset now)
    {
        _resumeAfterReaction = CatState.Idle;
        return SetState(CatState.DragDrop, now);
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

    public CatActionTransition TrackMouse(DateTimeOffset now, CatActionId? actionId = null)
    {
        _resumeAfterReaction = CatState.Idle;
        return SetState(CatState.MouseTrack, now, actionId ?? CatActionId.MouseTrack);
    }

    public CatActionTransition RetargetMouse(CatActionId actionId)
    {
        if (Current?.State is not CatState.MouseTrack)
        {
            throw new InvalidOperationException("Mouse tracking can only be retargeted while the cat is tracking the mouse.");
        }

        Current = Current with { ActionId = actionId };
        return Current;
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

    public CatActionTransition? StartleFromWindow(DateTimeOffset now)
    {
        if (Current?.State is CatState.DragLift or CatState.DragHold or CatState.DragDrop or CatState.PetReact or CatState.MouseTrack)
        {
            return null;
        }

        _resumeAfterReaction = CatState.Idle;
        return SetState(CatState.WindowStartle, now);
    }

    public CatActionTransition AvoidWindow(DateTimeOffset now)
    {
        _resumeAfterReaction = CatState.Idle;
        return SetState(CatState.WindowAvoid, now);
    }

    public CatActionTransition? VisitTaskbar(DateTimeOffset now, bool lie)
    {
        if (QuietMode || Current?.State is not CatState.Idle and not CatState.EdgePause)
        {
            return null;
        }

        _resumeAfterReaction = CatState.Idle;
        return SetState(CatState.TaskbarVisit, now, lie ? CatActionId.TaskbarLie : CatActionId.TaskbarSit);
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
            CatState.DragLift => SetState(CatState.DragHold, now),
            CatState.DragHold => SetState(_resumeAfterReaction, now),
            CatState.DragDrop => SetState(_resumeAfterReaction, now),
            CatState.MouseNotice => SetState(_resumeAfterReaction, now),
            CatState.MouseTrack => SetState(_resumeAfterReaction, now),
            CatState.WindowLinger => SetState(_resumeAfterReaction, now),
            CatState.WindowStartle => SetState(CatState.WindowAvoid, now),
            CatState.WindowAvoid => SetState(_resumeAfterReaction, now),
            CatState.TaskbarVisit => SetState(_resumeAfterReaction, now),
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
        var bucket = CatTimeBucketResolver.Resolve(now);
        var weights = BehaviorSettings.ResolveWeights(bucket, HabitProfile);
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

    private CatActionTransition SetState(CatState state, DateTimeOffset now, CatActionId? overrideActionId = null)
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
            CatState.DragLift => (CatActionId.DragLift, _options.DragLiftDuration),
            CatState.DragHold => (CatActionId.DragHold, _options.DragHoldDuration),
            CatState.DragDrop => (CatActionId.DragDrop, _options.DragDropDuration),
            CatState.MouseNotice => (CatActionId.MouseNotice, _options.MouseNoticeDuration),
            CatState.MouseTrack => (overrideActionId ?? CatActionId.MouseTrack, _options.MouseTrackDuration),
            CatState.WindowLinger => (CatActionId.WindowLinger, _options.WindowLingerDuration),
            CatState.WindowStartle => (CatActionId.WindowStartle, _options.WindowStartleDuration),
            CatState.WindowAvoid => (CatActionId.WindowAvoid, _options.WindowAvoidDuration),
            CatState.TaskbarVisit => (overrideActionId ?? CatActionId.TaskbarSit, _options.TaskbarVisitDuration),
            CatState.ObservationReact => (_observationActionId, _options.ObservationReactionDuration),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        Current = new CatActionTransition(state, actionId, now, now + duration);
        return Current;
    }
}
