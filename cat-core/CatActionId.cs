namespace MyCat.CatCore;

public readonly record struct CatActionId(string Value)
{
    public static readonly CatActionId IdleSit = new("idle_sit");
    public static readonly CatActionId RestSleep = new("rest_sleep");
    public static readonly CatActionId WalkSlow = new("walk_slow");
    public static readonly CatActionId WakeStretch = new("wake_stretch");
    public static readonly CatActionId EdgeStop = new("edge_stop");
    public static readonly CatActionId PetReact = new("pet_react");
    public static readonly CatActionId DragSettle = new("drag_settle");
    public static readonly CatActionId MouseNotice = new("mouse_notice");
    public static readonly CatActionId WindowLinger = new("window_linger");
    public static readonly CatActionId ObservationRest = new("observation_rest");
    public static readonly CatActionId ObservationActivity = new("observation_activity");
    public static readonly CatActionId ObservationAccompany = new("observation_accompany");

    public override string ToString() => Value;
}
