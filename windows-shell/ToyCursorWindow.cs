using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;
using WpfImage = System.Windows.Controls.Image;

namespace MyCat.WindowsShell;

internal sealed class ToyCursorWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;

    public ToyCursorWindow(string toyPath, double size)
    {
        Width = size;
        Height = size;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        IsHitTestVisible = false;
        Focusable = false;

        Content = new WpfImage
        {
            Source = LoadToyImage(toyPath),
            Stretch = Stretch.Uniform,
            Width = size,
            Height = size,
            IsHitTestVisible = false
        };

        SourceInitialized += (_, _) => MakeClickThrough();
    }

    public void MoveCenterTo(Point center, Rect bounds)
    {
        Left = Math.Clamp(center.X - (Width / 2), bounds.Left, bounds.Right - Width);
        Top = Math.Clamp(center.Y - (Height / 2), bounds.Top, bounds.Bottom - Height);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        Owner?.Activate();
    }

    private static ImageSource LoadToyImage(string toyPath)
    {
        if (!File.Exists(toyPath))
        {
            throw new FileNotFoundException("The yarn bell toy image is missing.", toyPath);
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(toyPath, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void MakeClickThrough()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style | WsExTransparent | WsExToolWindow | WsExNoActivate));
    }

    private static IntPtr GetWindowLongPtr(IntPtr handle, int index) =>
        IntPtr.Size == 8
            ? GetWindowLongPtr64(handle, index)
            : new IntPtr(GetWindowLong32(handle, index));

    private static IntPtr SetWindowLongPtr(IntPtr handle, int index, IntPtr value) =>
        IntPtr.Size == 8
            ? SetWindowLongPtr64(handle, index, value)
            : new IntPtr(SetWindowLong32(handle, index, value.ToInt32()));

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr handle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr handle, int index, int value);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr handle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr handle, int index, IntPtr value);
}
