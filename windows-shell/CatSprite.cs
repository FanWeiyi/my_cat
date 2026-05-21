using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

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
        ToolTip = "Pet the cat";
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
                DrawSleeping(drawingContext, FrameKey == "sleep_breathe");
                break;
            case "walk_left":
            case "walk_right":
                DrawWalking(drawingContext, FrameKey == "walk_right");
                break;
            case "pet_squish":
            case "pet_lift":
                DrawPetting(drawingContext, FrameKey == "pet_lift");
                break;
            default:
                DrawSitting(drawingContext, FrameKey == "sit_blink");
                break;
        }

        if (FacingLeft)
        {
            drawingContext.Pop();
        }
    }

    private static void DrawSitting(DrawingContext dc, bool blinking)
    {
        DrawShadow(dc, 24, 144, 132, 12);
        dc.DrawGeometry(null, Outline, Tail(new Point(42, 118), new Point(18, 82), new Point(40, 62)));
        dc.DrawEllipse(Fur, Outline, new Point(94, 113), 47, 39);
        dc.DrawEllipse(Cream, null, new Point(105, 121), 27, 25);
        dc.DrawEllipse(Fur, Outline, new Point(107, 68), 42, 38);
        DrawEars(dc, 78, 43, 135, 45);
        dc.DrawEllipse(Cream, null, new Point(117, 78), 20, 16);
        DrawFace(dc, 107, 72, blinking);
        dc.DrawEllipse(Cream, Outline, new Point(80, 142), 17, 8);
        dc.DrawEllipse(Cream, Outline, new Point(122, 143), 17, 8);
    }

    private static void DrawSleeping(DrawingContext dc, bool breathing)
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

    private static void DrawPetting(DrawingContext dc, bool lifted)
    {
        var headLift = lifted ? -9 : 4;
        DrawShadow(dc, 28, 146, 130, 12);
        dc.DrawGeometry(null, Outline, Tail(new Point(46, 121), new Point(18, 92), new Point(38, 67)));
        dc.DrawEllipse(Fur, Outline, new Point(94, lifted ? 112 : 120), lifted ? 44 : 51, lifted ? 36 : 31);
        dc.DrawEllipse(Cream, null, new Point(104, lifted ? 120 : 126), 26, 17);
        dc.DrawEllipse(Fur, Outline, new Point(108, 72 + headLift), 44, lifted ? 41 : 34);
        DrawEars(dc, 77, 47 + headLift, 138, 47 + headLift);
        dc.DrawEllipse(Cream, null, new Point(118, 82 + headLift), 21, 15);
        DrawFace(dc, 108, 76 + headLift, false);
        dc.DrawEllipse(Cream, Outline, new Point(80, 143), 18, 8);
        dc.DrawEllipse(Cream, Outline, new Point(124, 143), 18, 8);
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
