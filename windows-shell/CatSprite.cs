using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Cursors = System.Windows.Input.Cursors;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;

namespace MyCat.WindowsShell;

internal sealed class CatSprite : FrameworkElement
{
    private static readonly Brush Fur = Brush("#D68B43");
    private static readonly Brush Cream = Brush("#FFF1D7");
    private static readonly Brush Pink = Brush("#E99AA3");
    private static readonly Brush Ink = Brush("#45362E");
    private static readonly Pen Outline = Pen("#45362E", 3);
    private string _frameKey = "sit_open";
    private bool _facingLeft = true;

    public CatSprite()
    {
        Cursor = Cursors.Hand;
        ToolTip = "摸摸小猫";
        Width = 184;
        Height = 168;
        SnapsToDevicePixels = true;
    }

    public string FrameKey
    {
        get => _frameKey;
        set
        {
            _frameKey = value;
            InvalidateVisual();
        }
    }

    public bool FacingLeft
    {
        get => _facingLeft;
        set
        {
            _facingLeft = value;
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (FacingLeft)
        {
            drawingContext.PushTransform(new ScaleTransform(-1, 1, RenderSize.Width / 2, RenderSize.Height / 2));
        }

        switch (FrameKey)
        {
            case "sleep_low":
            case "sleep_breathe":
            case "sleep_dream":
                DrawSleeping(drawingContext, FrameKey == "sleep_breathe", FrameKey == "sleep_dream");
                break;
            case "walk_left":
            case "walk_right":
                DrawWalking(drawingContext, FrameKey == "walk_right");
                break;
            case "wake_low":
            case "wake_stretch":
            case "wake_settle":
                DrawWaking(drawingContext, FrameKey);
                break;
            case "edge_brake":
            case "edge_watch":
                DrawEdgeStop(drawingContext, FrameKey == "edge_watch");
                break;
            case "pet_squish":
            case "pet_lift":
            case "pet_nuzzle":
            case "note_nuzzle":
                DrawPetting(drawingContext, FrameKey is "pet_lift" or "note_nuzzle", FrameKey is "pet_nuzzle" or "note_nuzzle");
                break;
            case "settle_drop":
            case "settle_blink":
                DrawSettling(drawingContext, FrameKey == "settle_blink");
                break;
            case "notice_glance":
            case "notice_focus":
                DrawNoticing(drawingContext, FrameKey == "notice_focus");
                break;
            case "window_perch":
            case "window_watch":
                DrawWindowLinger(drawingContext, FrameKey == "window_watch");
                break;
            case "note_yawn":
                DrawYawning(drawingContext);
                break;
            case "note_perk":
                DrawPerked(drawingContext);
                break;
            default:
                DrawSitting(drawingContext, FrameKey == "sit_blink", FrameKey == "sit_tail");
                break;
        }

        if (FacingLeft)
        {
            drawingContext.Pop();
        }
    }

    private static void DrawSitting(DrawingContext dc, bool blinking, bool tailLifted = false)
    {
        DrawShadow(dc, 24, 144, 132, 12);
        dc.DrawGeometry(null, Outline, Tail(
            new Point(42, 118),
            new Point(18, tailLifted ? 70 : 82),
            new Point(tailLifted ? 51 : 40, tailLifted ? 48 : 62)));
        dc.DrawEllipse(Fur, Outline, new Point(94, 113), 47, 39);
        dc.DrawEllipse(Cream, null, new Point(105, 121), 27, 25);
        dc.DrawEllipse(Fur, Outline, new Point(107, 68), 42, 38);
        DrawEars(dc, 78, 43, 135, 45);
        dc.DrawEllipse(Cream, null, new Point(117, 78), 20, 16);
        DrawFace(dc, 107, 72, blinking);
        dc.DrawEllipse(Cream, Outline, new Point(80, 142), 17, 8);
        dc.DrawEllipse(Cream, Outline, new Point(122, 143), 17, 8);
        DrawCheeks(dc, 107, 72);
    }

    private static void DrawSleeping(DrawingContext dc, bool breathing, bool dreaming = false)
    {
        var lift = breathing ? -3 : 0;
        DrawShadow(dc, 28, 144, 130, 11);
        dc.DrawGeometry(null, Outline, Tail(new Point(45, 127), new Point(25, 103), new Point(54, 94)));
        dc.DrawEllipse(Fur, Outline, new Point(100, 125 + lift), 58, 25 - lift);
        dc.DrawEllipse(Cream, null, new Point(104, 129 + lift), 36, 14);
        dc.DrawEllipse(Fur, Outline, new Point(65, 116 + lift), 32, 28);
        DrawEars(dc, 42, 94 + lift, 80, 93 + lift);
        dc.DrawLine(Outline, new Point(54, 116 + lift), new Point(65, 119 + lift));
        dc.DrawLine(Outline, new Point(74, 118 + lift), new Point(84, 115 + lift));
        dc.DrawEllipse(Pink, null, new Point(71, 126 + lift), 4, 3);
        dc.DrawEllipse(Cream, Outline, new Point(134, 144), 22, 7);
        if (dreaming)
        {
            dc.DrawEllipse(null, Outline, new Point(40, 78), 4, 4);
            dc.DrawEllipse(null, Outline, new Point(30, 64), 6, 6);
        }
    }

    private static void DrawWalking(DrawingContext dc, bool frontStep)
    {
        DrawShadow(dc, 20, 145, 144, 10);
        dc.DrawGeometry(null, Outline, Tail(new Point(42, 105), new Point(10, 78), new Point(27, 54)));
        dc.DrawEllipse(Fur, Outline, new Point(91, 108), 56, 32);
        dc.DrawEllipse(Cream, null, new Point(101, 115), 34, 18);
        dc.DrawEllipse(Fur, Outline, new Point(133, 79), 33, 31);
        DrawEars(dc, 110, 58, 151, 56);
        DrawFace(dc, 133, 79, false);
        DrawLeg(dc, 59, 124, frontStep ? 141 : 134);
        DrawLeg(dc, 87, 125, frontStep ? 134 : 142);
        DrawLeg(dc, 119, 123, frontStep ? 141 : 134);
        DrawLeg(dc, 142, 120, frontStep ? 133 : 140);
    }

    private static void DrawPetting(DrawingContext dc, bool lifted, bool nuzzling = false)
    {
        var headLift = lifted ? -9 : 4;
        var headShift = nuzzling ? 7 : 0;
        DrawShadow(dc, 28, 146, 130, 12);
        dc.DrawGeometry(null, Outline, Tail(new Point(46, 121), new Point(18, 92), new Point(38, 67)));
        dc.DrawEllipse(Fur, Outline, new Point(94, lifted ? 112 : 120), lifted ? 44 : 51, lifted ? 36 : 31);
        dc.DrawEllipse(Cream, null, new Point(104, lifted ? 120 : 126), 26, 17);
        dc.DrawEllipse(Fur, Outline, new Point(108 + headShift, 72 + headLift), 44, lifted ? 41 : 34);
        DrawEars(dc, 77 + headShift, 47 + headLift, 138 + headShift, 47 + headLift);
        dc.DrawEllipse(Cream, null, new Point(118 + headShift, 82 + headLift), 21, 15);
        DrawFace(dc, 108 + headShift, 76 + headLift, nuzzling);
        dc.DrawEllipse(Cream, Outline, new Point(80, 143), 18, 8);
        dc.DrawEllipse(Cream, Outline, new Point(124, 143), 18, 8);
        DrawCheeks(dc, 108 + headShift, 76 + headLift);
    }

    private static void DrawWaking(DrawingContext dc, string frameKey)
    {
        if (frameKey == "wake_low")
        {
            DrawSleeping(dc, false);
            return;
        }

        var stretched = frameKey == "wake_stretch";
        DrawShadow(dc, 24, 145, 136, 11);
        dc.DrawGeometry(null, Outline, Tail(new Point(46, 116), new Point(18, 78), new Point(35, 53)));
        dc.DrawEllipse(Fur, Outline, new Point(94, stretched ? 111 : 117), stretched ? 59 : 49, stretched ? 27 : 35);
        dc.DrawEllipse(Cream, null, new Point(102, stretched ? 117 : 124), stretched ? 36 : 28, 16);
        dc.DrawEllipse(Fur, Outline, new Point(130, stretched ? 84 : 76), 34, 32);
        DrawEars(dc, 108, stretched ? 61 : 52, 151, stretched ? 61 : 52);
        DrawFace(dc, 130, stretched ? 85 : 78, !stretched);
        DrawLeg(dc, 59, 124, stretched ? 144 : 138);
        DrawLeg(dc, 88, 126, stretched ? 143 : 138);
        DrawLeg(dc, 130, 115, stretched ? 136 : 141);
    }

    private static void DrawEdgeStop(DrawingContext dc, bool watching)
    {
        DrawShadow(dc, 20, 145, 144, 10);
        dc.DrawGeometry(null, Outline, Tail(new Point(41, 106), new Point(12, watching ? 62 : 76), new Point(29, watching ? 44 : 54)));
        dc.DrawEllipse(Fur, Outline, new Point(91, watching ? 110 : 108), 54, watching ? 34 : 31);
        dc.DrawEllipse(Cream, null, new Point(101, 117), 33, 17);
        dc.DrawEllipse(Fur, Outline, new Point(135, watching ? 76 : 81), 34, watching ? 35 : 30);
        DrawEars(dc, 111, watching ? 49 : 59, 153, watching ? 47 : 57);
        DrawFace(dc, 135, watching ? 77 : 81, false);
        DrawLeg(dc, 61, 124, 140);
        DrawLeg(dc, 91, 125, 140);
        DrawLeg(dc, 124, 122, 140);
        DrawLeg(dc, 145, 120, watching ? 136 : 140);
    }

    private static void DrawSettling(DrawingContext dc, bool blink)
    {
        dc.PushTransform(new TranslateTransform(0, blink ? 0 : -6));
        DrawSitting(dc, blink, !blink);
        dc.Pop();
    }

    private static void DrawNoticing(DrawingContext dc, bool focused)
    {
        DrawShadow(dc, 24, 144, 132, 12);
        dc.DrawGeometry(null, Outline, Tail(new Point(42, 118), new Point(20, 82), new Point(42, 58)));
        dc.DrawEllipse(Fur, Outline, new Point(92, 113), 47, 39);
        dc.DrawEllipse(Cream, null, new Point(103, 121), 27, 25);
        dc.DrawEllipse(Fur, Outline, new Point(focused ? 116 : 110, focused ? 64 : 68), 43, 39);
        DrawEars(dc, focused ? 86 : 80, focused ? 37 : 43, focused ? 144 : 138, focused ? 39 : 45);
        dc.DrawEllipse(Cream, null, new Point(focused ? 126 : 120, focused ? 75 : 78), 20, 16);
        DrawFace(dc, focused ? 116 : 110, focused ? 70 : 72, false);
        dc.DrawEllipse(Cream, Outline, new Point(80, 142), 17, 8);
        dc.DrawEllipse(Cream, Outline, new Point(122, 143), 17, 8);
    }

    private static void DrawWindowLinger(DrawingContext dc, bool watching)
    {
        dc.DrawRoundedRectangle(Brush("#8E9EB4"), null, new Rect(25, 142, 132, 8), 3, 3);
        DrawShadow(dc, 31, 145, 120, 8);
        dc.DrawGeometry(null, Outline, Tail(new Point(50, 119), new Point(22, 92), new Point(45, watching ? 59 : 68)));
        dc.DrawEllipse(Fur, Outline, new Point(94, 116), 48, 34);
        dc.DrawEllipse(Cream, null, new Point(104, 122), 27, 18);
        dc.DrawEllipse(Fur, Outline, new Point(119, watching ? 68 : 73), 40, 37);
        DrawEars(dc, 90, watching ? 41 : 47, 146, watching ? 42 : 48);
        dc.DrawEllipse(Cream, null, new Point(127, watching ? 79 : 83), 19, 14);
        DrawFace(dc, 119, watching ? 72 : 77, !watching);
        dc.DrawEllipse(Cream, Outline, new Point(78, 142), 18, 7);
        dc.DrawEllipse(Cream, Outline, new Point(126, 142), 18, 7);
    }

    private static void DrawYawning(DrawingContext dc)
    {
        DrawSitting(dc, false, true);
        dc.DrawEllipse(Pink, Outline, new Point(108, 92), 9, 7);
    }

    private static void DrawPerked(DrawingContext dc)
    {
        dc.PushTransform(new TranslateTransform(0, -5));
        DrawSitting(dc, false, true);
        dc.DrawEllipse(Pink, null, new Point(146, 39), 3, 3);
        dc.DrawEllipse(Pink, null, new Point(153, 31), 2, 2);
        dc.Pop();
    }

    private static void DrawFace(DrawingContext dc, double x, double y, bool blinking)
    {
        if (blinking)
        {
            dc.DrawLine(Outline, new Point(x - 20, y - 2), new Point(x - 10, y));
            dc.DrawLine(Outline, new Point(x + 11, y), new Point(x + 21, y - 2));
        }
        else
        {
            dc.DrawEllipse(Ink, null, new Point(x - 15, y - 1), 4, 6);
            dc.DrawEllipse(Ink, null, new Point(x + 16, y - 1), 4, 6);
        }

        dc.DrawEllipse(Pink, null, new Point(x + 1, y + 9), 4, 3);
        dc.DrawLine(Outline, new Point(x + 1, y + 12), new Point(x - 5, y + 17));
        dc.DrawLine(Outline, new Point(x + 1, y + 12), new Point(x + 8, y + 16));
    }

    private static void DrawCheeks(DrawingContext dc, double x, double y)
    {
        dc.DrawEllipse(Brush("#44E99AA3"), null, new Point(x - 28, y + 11), 6, 3);
        dc.DrawEllipse(Brush("#44E99AA3"), null, new Point(x + 29, y + 11), 6, 3);
    }

    private static void DrawEars(DrawingContext dc, double leftX, double leftY, double rightX, double rightY)
    {
        dc.DrawGeometry(Fur, Outline, Triangle(leftX, leftY + 23, leftX + 13, leftY - 7, leftX + 28, leftY + 19));
        dc.DrawGeometry(Fur, Outline, Triangle(rightX - 25, rightY + 18, rightX - 9, rightY - 8, rightX + 4, rightY + 23));
    }

    private static void DrawLeg(DrawingContext dc, double x, double top, double foot)
    {
        dc.DrawLine(Outline, new Point(x, top), new Point(x + 2, foot));
        dc.DrawEllipse(Cream, Outline, new Point(x + 4, foot + 2), 9, 4);
    }

    private static void DrawShadow(DrawingContext dc, double left, double top, double width, double height)
    {
        dc.DrawEllipse(Brush("#332D2922"), null, new Point(left + (width / 2), top + (height / 2)), width / 2, height / 2);
    }

    private static Geometry Tail(Point start, Point control, Point end)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(start, false, false);
        context.BezierTo(control, control, end, true, false);
        return geometry;
    }

    private static Geometry Triangle(double x1, double y1, double x2, double y2, double x3, double y3)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(new Point(x1, y1), true, true);
        context.LineTo(new Point(x2, y2), true, false);
        context.LineTo(new Point(x3, y3), true, false);
        return geometry;
    }

    private static SolidColorBrush Brush(string color)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
        brush.Freeze();
        return brush;
    }

    private static Pen Pen(string color, double thickness)
    {
        var pen = new Pen(Brush(color), thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };

        pen.Freeze();
        return pen;
    }
}
