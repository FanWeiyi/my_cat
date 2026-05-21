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

    public CatActionTransition Start(DateTimeOffset now)
    {
        return SetState(CatState.Idle, now);
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
            _ => SetState(ChooseAutomaticState(), now)
        };
    }

    private CatState ChooseAutomaticState()
    {
        var sample = _sample();

        if (sample < 0.4)
        {
            return CatState.Idle;
        }

        if (sample < 0.75)
        {
            return CatState.Rest;
        }

        return CatState.Walk;
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
