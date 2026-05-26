namespace MyCat.CatCore;

public sealed record CatInteractionMetrics(
    int ClickCount,
    int DragCount,
    int TellCount,
    int QuietModeEnableCount,
    int MouseNoticeCount,
    int WindowLingerCount,
    int MouseTrackCount,
    int DragLiftCount,
    int DragDropCount,
    int WindowAvoidCount,
    int TaskbarVisitCount,
    int WindowPeekCount)
{
    public static readonly CatInteractionMetrics Empty = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    public CatInteractionMetrics CountClick() => this with { ClickCount = ClickCount + 1 };

    public CatInteractionMetrics CountDrag() => this with { DragCount = DragCount + 1 };

    public CatInteractionMetrics CountTell() => this with { TellCount = TellCount + 1 };

    public CatInteractionMetrics CountQuietModeEnable() =>
        this with { QuietModeEnableCount = QuietModeEnableCount + 1 };

    public CatInteractionMetrics CountMouseNotice() => this with { MouseNoticeCount = MouseNoticeCount + 1 };

    public CatInteractionMetrics CountWindowLinger() => this with { WindowLingerCount = WindowLingerCount + 1 };

    public CatInteractionMetrics CountMouseTrack() => this with { MouseTrackCount = MouseTrackCount + 1 };

    public CatInteractionMetrics CountDragLift() => this with { DragLiftCount = DragLiftCount + 1 };

    public CatInteractionMetrics CountDragDrop() => this with { DragDropCount = DragDropCount + 1 };

    public CatInteractionMetrics CountWindowAvoid() => this with { WindowAvoidCount = WindowAvoidCount + 1 };

    public CatInteractionMetrics CountTaskbarVisit() => this with { TaskbarVisitCount = TaskbarVisitCount + 1 };

    public CatInteractionMetrics CountWindowPeek() => this with { WindowPeekCount = WindowPeekCount + 1 };
}
