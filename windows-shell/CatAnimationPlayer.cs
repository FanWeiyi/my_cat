using System.Windows.Threading;
using MyCat.CatAssets;
using MyCat.CatCore;

namespace MyCat.WindowsShell;

internal sealed class CatAnimationPlayer : IDisposable
{
    private readonly CatAnimationCatalog _catalog;
    private readonly CatSprite _sprite;
    private readonly DispatcherTimer _frameTimer;
    private CatAnimationClip? _clip;
    private int _frameIndex;

    public CatAnimationPlayer(CatAnimationCatalog catalog, CatSprite sprite)
    {
        _catalog = catalog;
        _sprite = sprite;
        _frameTimer = new DispatcherTimer(DispatcherPriority.Render);
        _frameTimer.Tick += HandleFrameElapsed;
    }

    public void Play(CatActionId actionId)
    {
        _clip = _catalog.Get(actionId);
        _frameIndex = 0;
        ShowFrame();
        _frameTimer.Start();
    }

    public void Dispose()
    {
        _frameTimer.Stop();
        _frameTimer.Tick -= HandleFrameElapsed;
    }

    private void HandleFrameElapsed(object? sender, EventArgs e)
    {
        if (_clip is null)
        {
            return;
        }

        if (_frameIndex == _clip.Frames.Count - 1 && !_clip.Loop)
        {
            _frameTimer.Stop();
            return;
        }

        _frameIndex = (_frameIndex + 1) % _clip.Frames.Count;
        ShowFrame();
    }

    private void ShowFrame()
    {
        if (_clip is null)
        {
            return;
        }

        var frame = _clip.Frames[_frameIndex];
        _sprite.FrameKey = frame.Key;
        _frameTimer.Interval = frame.Duration;
    }
}

