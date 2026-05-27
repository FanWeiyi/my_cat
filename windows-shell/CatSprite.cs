using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Cursors = System.Windows.Input.Cursors;
using Rect = System.Windows.Rect;

namespace MyCat.WindowsShell;

internal sealed class CatSprite : FrameworkElement
{
    private readonly Dictionary<string, ImageSource> _frameCache = new(StringComparer.OrdinalIgnoreCase);
    private string? _frameKey;
    private ImageSource? _frame;

    public CatSprite()
    {
        Cursor = Cursors.Hand;
        ToolTip = "摸摸小猫";
        Width = 184;
        Height = 168;
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
    }

    public string? FrameKey
    {
        get => _frameKey;
        set
        {
            if (_frameKey == value)
            {
                return;
            }

            _frameKey = value;
            _frame = string.IsNullOrWhiteSpace(value)
                ? null
                : _frameCache.GetValueOrDefault(value) ?? LoadFrame(value);
            InvalidateVisual();
        }
    }

    public bool FacingLeft { get; set; } = true;

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (_frame is not null)
        {
            drawingContext.DrawImage(_frame, new Rect(RenderSize));
        }
    }

    private ImageSource LoadFrame(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("A cat animation frame is missing.", path);
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            _frameCache.Add(path, image);
            return image;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or UriFormatException)
        {
            AppLogger.LogException("AssetFrameLoadFailed", ex);
            throw;
        }
    }
}
