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
    public static readonly CatActionId DragLift = new("drag_lift");
    public static readonly CatActionId DragHold = new("drag_hold");
    public static readonly CatActionId DragDrop = new("drag_drop");
    public static readonly CatActionId MouseNotice = new("mouse_notice");
    public static readonly CatActionId MouseTrack = new("mouse_track");
    public static readonly CatActionId MouseTrackLeft = new("mouse_track_left");
    public static readonly CatActionId MouseTrackRight = new("mouse_track_right");
    public static readonly CatActionId MouseTrackUp = new("mouse_track_up");
    public static readonly CatActionId MouseTrackDown = new("mouse_track_down");
    public static readonly CatActionId WindowLinger = new("window_linger");
    public static readonly CatActionId WindowStartle = new("window_startle");
    public static readonly CatActionId WindowAvoid = new("window_avoid");
    public static readonly CatActionId TaskbarSit = new("taskbar_sit");
    public static readonly CatActionId TaskbarLie = new("taskbar_lie");
    public static readonly CatActionId ObservationRest = new("observation_rest");
    public static readonly CatActionId ObservationActivity = new("observation_activity");
    public static readonly CatActionId ObservationAccompany = new("observation_accompany");

    public override string ToString() => Value;
}
