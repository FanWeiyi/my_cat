using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MyCat.CatAssets;
using MyCat.CatCore;

namespace MyCat.WindowsShell;

internal sealed class DesktopCatWindow : Window
{
    private const double SafeMargin = 18;
    private readonly CatAnimationPlayer _animationPlayer;
    private readonly CatBehaviorController _behavior = new();
    private readonly CatSprite _sprite = new();
    private readonly DispatcherTimer _tickTimer;
    private readonly TrayIconHost _trayIcon;
    private int _walkDirection = -1;

    public DesktopCatWindow()
    {
        Title = "My Cat";
        Width = 184;
        Height = 168;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        Content = _sprite;

        _animationPlayer = new CatAnimationPlayer(new CatAnimationCatalog(), _sprite);
        _trayIcon = new TrayIconHost(() => Dispatcher.Invoke(Close));
        _tickTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };

        Loaded += HandleLoaded;
        Closed += HandleClosed;
        _tickTimer.Tick += HandleTick;
        _sprite.MouseLeftButtonUp += HandlePet;
    }

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        PositionNearWorkArea();
        Apply(_behavior.Start(DateTimeOffset.Now));
        _tickTimer.Start();
    }

    private void HandleClosed(object? sender, EventArgs e)
    {
        _tickTimer.Stop();
        _animationPlayer.Dispose();
        _trayIcon.Dispose();
    }

    private void HandlePet(object sender, MouseButtonEventArgs e)
    {
        Apply(_behavior.Pet(DateTimeOffset.Now));
        e.Handled = true;
    }

    private void HandleTick(object? sender, EventArgs e)
    {
        if (_behavior.Current?.State is CatState.Walk)
        {
            MoveAlongWorkArea();
        }

        var transition = _behavior.Advance(DateTimeOffset.Now);
        if (transition is not null)
        {
            Apply(transition);
        }
    }

    private void Apply(CatActionTransition transition)
    {
        if (transition.State is CatState.Walk)
        {
            _walkDirection = PickWalkDirection();
        }

        _sprite.FacingLeft = _walkDirection < 0;
        _animationPlayer.Play(transition.ActionId);
    }

    private void PositionNearWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        Left = Math.Max(workArea.Left + SafeMargin, workArea.Right - Width - 112);
        Top = Math.Max(workArea.Top + SafeMargin, workArea.Bottom - Height - SafeMargin);
    }

    private int PickWalkDirection()
    {
        var workArea = SystemParameters.WorkArea;
        var midpoint = workArea.Left + (workArea.Width / 2);
        return Left > midpoint ? -1 : 1;
    }

    private void MoveAlongWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        var minimumLeft = workArea.Left + SafeMargin;
        var maximumLeft = workArea.Right - Width - SafeMargin;
        var candidate = Left + (_walkDirection * 1.9);

        if (candidate <= minimumLeft || candidate >= maximumLeft)
        {
            _walkDirection *= -1;
            candidate = Math.Clamp(candidate, minimumLeft, maximumLeft);
            _sprite.FacingLeft = _walkDirection < 0;
        }

        Left = candidate;
    }
}

