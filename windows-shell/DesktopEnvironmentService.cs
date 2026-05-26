using System.Runtime.InteropServices;
using System.Windows;
using Point = System.Windows.Point;

namespace MyCat.WindowsShell;

internal sealed class DesktopEnvironmentService
{
    public Point MouseScreenPosition => new(NativeMethods.GetCursorPos(out var point) ? point.X : 0, point.Y);

    public Rect FullScreenBounds => new(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);

    public Rect WorkAreaBounds => SystemParameters.WorkArea;

    public Rect? BottomTaskbarBounds
    {
        get
        {
            var screen = FullScreenBounds;
            var workArea = WorkAreaBounds;
            var height = screen.Bottom - workArea.Bottom;
            return height > 1
                ? new Rect(screen.Left, workArea.Bottom, screen.Width, height)
                : null;
        }
    }

    public DesktopWindowSnapshot? GetForegroundWindowSnapshot(IntPtr excludedWindowHandle, Func<Point, Point> fromDevicePoint)
    {
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero
            || foreground == excludedWindowHandle
            || NativeMethods.IsIconic(foreground)
            || !NativeMethods.GetWindowRect(foreground, out var nativeRect))
        {
            return null;
        }

        var topLeft = fromDevicePoint(new Point(nativeRect.Left, nativeRect.Top));
        var bottomRight = fromDevicePoint(new Point(nativeRect.Right, nativeRect.Bottom));
        var rect = new Rect(topLeft, bottomRight);
        return rect.Width > 1 && rect.Height > 1
            ? new DesktopWindowSnapshot(foreground, rect)
            : null;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr windowHandle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr windowHandle, out NativeRect rect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out NativePoint point);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}

internal readonly record struct DesktopWindowSnapshot(IntPtr Handle, Rect Rect);
