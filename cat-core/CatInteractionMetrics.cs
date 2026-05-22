namespace MyCat.CatCore;

public sealed record CatInteractionMetrics(
    int ClickCount,
    int DragCount,
    int TellCount,
    int QuietModeEnableCount,
    int MouseNoticeCount,
    int WindowLingerCount)
{
    public static readonly CatInteractionMetrics Empty = new(0, 0, 0, 0, 0, 0);

    public CatInteractionMetrics CountClick() => this with { ClickCount = ClickCount + 1 };

    public CatInteractionMetrics CountDrag() => this with { DragCount = DragCount + 1 };

    public CatInteractionMetrics CountTell() => this with { TellCount = TellCount + 1 };

    public CatInteractionMetrics CountQuietModeEnable() =>
        this with { QuietModeEnableCount = QuietModeEnableCount + 1 };

    public CatInteractionMetrics CountMouseNotice() => this with { MouseNoticeCount = MouseNoticeCount + 1 };

    public CatInteractionMetrics CountWindowLinger() => this with { WindowLingerCount = WindowLingerCount + 1 };
}
