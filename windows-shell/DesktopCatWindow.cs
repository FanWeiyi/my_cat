using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using MyCat.CatAssets;
using MyCat.CatCore;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;

namespace MyCat.WindowsShell;

internal sealed class DesktopCatWindow : Window
{
    private const double SafeMargin = 18;
    private const double WalkStep = 5.6;
    private static readonly TimeSpan DragPlacementMemory = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan MouseNoticeCooldown = TimeSpan.FromSeconds(18);
    private static readonly TimeSpan WindowVisitCooldown = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan WindowVisitRetry = TimeSpan.FromSeconds(40);
    private readonly CatAnimationPlayer _animationPlayer;
    private readonly CatBehaviorController _behavior = new();
    private readonly ContextMenu _catMenu;
    private readonly JsonCatEventStore _eventStore = new(GetEventStorePath());
    private readonly JsonCatLearningStateStore _learningStateStore = new(GetLearningStateStorePath());
    private readonly JsonCatInteractionMetricsStore _metricsStore = new(GetMetricsStorePath());
    private readonly Border _feedbackBubble;
    private readonly TextBlock _feedbackText = new();
    private readonly Random _random = new();
    private readonly CatSprite _sprite = new();
    private readonly DispatcherTimer _feedbackTimer;
    private readonly DispatcherTimer _tickTimer;
    private readonly TrayIconHost _trayIcon;
    private CatLearningFeedbackTracker _learningFeedback = new();
    private CatInteractionMetrics _metrics = CatInteractionMetrics.Empty;
    private MenuItem? _quietModeMenuItem;
    private Point _dragPointerOffset;
    private Point _dragStartScreen;
    private Point _walkTarget;
    private Point? _preferredPlacement;
    private DateTimeOffset _preferredPlacementUntil;
    private DateTimeOffset _nextMouseNoticeAt;
    private DateTimeOffset _nextWindowInterestAt;
    private bool _pointerDown;
    private bool _dragging;
    private WalkPurpose _walkPurpose = WalkPurpose.Roam;

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

        _animationPlayer = new CatAnimationPlayer(new CatAnimationCatalog(), _sprite);
        _catMenu = CreateCatMenu();
        _sprite.ContextMenu = _catMenu;
        _feedbackBubble = CreateFeedbackBubble();
        Content = CreateCatSurface();
        _trayIcon = new TrayIconHost(
            eventType => Dispatcher.BeginInvoke(() => Record(eventType, CatEventSource.TrayMenu)),
            enabled => Dispatcher.BeginInvoke(() => SetQuietMode(enabled)),
            () => Dispatcher.Invoke(Close));
        _feedbackTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1.6)
        };
        _tickTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };

        Loaded += HandleLoaded;
        Closed += HandleClosed;
        _feedbackTimer.Tick += HandleFeedbackElapsed;
        _tickTimer.Tick += HandleTick;
        _sprite.MouseLeftButtonDown += HandleCatPointerDown;
        _sprite.MouseLeftButtonUp += HandleCatPointerUp;
        _sprite.MouseMove += HandleCatPointerMove;
        _sprite.MouseEnter += HandleCatMouseEnter;
    }

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        LoadLearningState();
        LoadMetrics();
        PositionNearWorkArea();
        var now = DateTimeOffset.Now;
        _walkTarget = new Point(Left, Top);
        _nextMouseNoticeAt = now + TimeSpan.FromSeconds(4);
        _nextWindowInterestAt = now + TimeSpan.FromSeconds(45);
        Apply(_behavior.Start(now));
        _tickTimer.Start();
    }

    private void HandleClosed(object? sender, EventArgs e)
    {
        _tickTimer.Stop();
        _feedbackTimer.Stop();
        _animationPlayer.Dispose();
        _trayIcon.Dispose();
    }

    private void HandleCatPointerDown(object sender, MouseButtonEventArgs e)
    {
        _catMenu.IsOpen = false;
        _pointerDown = true;
        _dragging = false;
        _dragStartScreen = GetPointerScreenPosition(e);
        _dragPointerOffset = e.GetPosition(this);
        _sprite.CaptureMouse();
        e.Handled = true;
    }

    private void HandleCatPointerMove(object sender, MouseEventArgs e)
    {
        if (!_pointerDown || e.LeftButton is not MouseButtonState.Pressed)
        {
            return;
        }

        var pointer = GetPointerScreenPosition(e);
        if (!_dragging && !HasPassedDragThreshold(pointer))
        {
            return;
        }

        _dragging = true;
        _sprite.Cursor = Cursors.SizeAll;
        var candidate = ClampToSafePosition(new Point(
            pointer.X - _dragPointerOffset.X,
            pointer.Y - _dragPointerOffset.Y));
        Left = candidate.X;
        Top = candidate.Y;
        e.Handled = true;
    }

    private void HandleCatPointerUp(object sender, MouseButtonEventArgs e)
    {
        if (!_pointerDown)
        {
            return;
        }

        _sprite.ReleaseMouseCapture();
        _sprite.Cursor = Cursors.Hand;
        _pointerDown = false;

        if (_dragging)
        {
            _dragging = false;
            RememberPlacement();
            Apply(_behavior.DragSettled(DateTimeOffset.Now));
            CountMetric(metrics => metrics.CountDrag());
            ShowFeedback("就待这儿");
            e.Handled = true;
            return;
        }

        Apply(_behavior.Pet(DateTimeOffset.Now));
        CountMetric(metrics => metrics.CountClick());
        OpenCatMenu();
        e.Handled = true;
    }

    private void HandleCatMouseEnter(object sender, MouseEventArgs e)
    {
        var now = DateTimeOffset.Now;
        if (_pointerDown || now < _nextMouseNoticeAt)
        {
            return;
        }

        var transition = _behavior.NoticeMouse(now);
        _nextMouseNoticeAt = now + MouseNoticeCooldown;
        if (transition is null)
        {
            return;
        }

        Apply(transition);
        CountMetric(metrics => metrics.CountMouseNotice());
    }

    private void HandleFeedbackElapsed(object? sender, EventArgs e)
    {
        _feedbackTimer.Stop();
        _feedbackBubble.Visibility = Visibility.Collapsed;
    }

    private void HandleTick(object? sender, EventArgs e)
    {
        if (_pointerDown)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        if (_behavior.Current?.State is CatState.Walk && MoveTowardWalkTarget())
        {
            SettleAfterWalk(now);
            return;
        }

        var transition = _behavior.Advance(now);
        if (transition is not null)
        {
            Apply(transition);
        }
    }

    private void Apply(CatActionTransition transition)
    {
        if (transition.State is CatState.Walk)
        {
            PickWalkTarget(transition.StartedAt);
        }

        if (_walkTarget.X != Left)
        {
            _sprite.FacingLeft = _walkTarget.X < Left;
        }

        _animationPlayer.Play(transition.ActionId);
    }

    private void SettleAfterWalk(DateTimeOffset now)
    {
        if (_walkPurpose is WalkPurpose.Window)
        {
            var windowPause = _behavior.LingerByWindow(now);
            if (windowPause is not null)
            {
                CountMetric(metrics => metrics.CountWindowLinger());
                Apply(windowPause);
                return;
            }
        }

        Apply(_behavior.ReachWalkEdge(now));
    }

    private Grid CreateCatSurface()
    {
        var surface = new Grid
        {
            Width = Width,
            Height = Height
        };

        surface.Children.Add(_sprite);
        surface.Children.Add(_feedbackBubble);
        return surface;
    }

    private ContextMenu CreateCatMenu()
    {
        var menu = new ContextMenu
        {
            Placement = PlacementMode.Top,
            PlacementTarget = _sprite,
            HasDropShadow = true,
            Background = new SolidColorBrush(Color.FromRgb(255, 248, 235)),
            Foreground = new SolidColorBrush(Color.FromRgb(69, 54, 46)),
            FontSize = 13
        };

        menu.Items.Add(MenuItem("摸摸它", () =>
        {
            Apply(_behavior.Pet(DateTimeOffset.Now));
            CountMetric(metrics => metrics.CountClick());
        }));

        var tellMenu = new MenuItem { Header = "告诉它" };
        tellMenu.Items.Add(MenuItem("我家猫在睡觉", () => Record(CatEventType.Rest, CatEventSource.DesktopCatMenu)));
        tellMenu.Items.Add(MenuItem("我家猫在玩", () => Record(CatEventType.Activity, CatEventSource.DesktopCatMenu)));
        tellMenu.Items.Add(MenuItem("我家猫在陪我", () => Record(CatEventType.Accompany, CatEventSource.DesktopCatMenu)));
        menu.Items.Add(tellMenu);
        _quietModeMenuItem = MenuItem("安静一会儿", () => SetQuietMode(!_behavior.QuietMode));
        menu.Items.Add(_quietModeMenuItem);

        return menu;
    }

    private static MenuItem MenuItem(string header, Action action)
    {
        var item = new MenuItem
        {
            Header = header,
            Padding = new Thickness(10, 5, 10, 5)
        };
        item.Click += (_, _) => action();
        return item;
    }

    private Border CreateFeedbackBubble()
    {
        _feedbackText.Foreground = Brushes.White;
        _feedbackText.FontSize = 13;
        _feedbackText.FontWeight = FontWeights.SemiBold;
        _feedbackText.TextAlignment = TextAlignment.Center;

        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(232, 87, 67, 55)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(118, 255, 242, 214)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 5, 0, 0),
            Padding = new Thickness(10, 4, 10, 4),
            Child = _feedbackText,
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false
        };
    }

    private void OpenCatMenu()
    {
        _catMenu.PlacementTarget = _sprite;
        _catMenu.IsOpen = true;
    }

    private void Record(CatEventType eventType, string source)
    {
        try
        {
            _eventStore.Append(CatObservationEvent.Create(eventType, DateTimeOffset.Now, source));
            var latest = _eventStore.ReadAll().Last();
            RefreshHabitProfile();
            CountMetric(metrics => metrics.CountTell());
            Apply(_behavior.ReactToObservation(eventType, DateTimeOffset.Now));
            var feedback = _learningFeedback.TryCreate(_behavior.HabitProfile, latest);
            if (feedback is not null)
            {
                SaveLearningState();
                ShowFeedback(feedback.Text);
                return;
            }

            ShowFeedback(eventType switch
            {
                CatEventType.Rest => "它记住困意了",
                CatEventType.Activity => "它听见玩心了",
                CatEventType.Accompany => "它靠近一点",
                _ => "已记下"
            });
        }
        catch (IOException)
        {
            ShowFeedback("这次没记上");
        }
        catch (UnauthorizedAccessException)
        {
            ShowFeedback("这次没记上");
        }
    }

    private void ShowFeedback(string text)
    {
        _feedbackText.Text = text;
        _feedbackBubble.Visibility = Visibility.Visible;
        _feedbackTimer.Stop();
        _feedbackTimer.Start();
    }

    private void LoadLearningState()
    {
        RefreshHabitProfile();
        _learningFeedback = new CatLearningFeedbackTracker(_learningStateStore.Read().SeenFeedbackKeys);
    }

    private void LoadMetrics()
    {
        try
        {
            _metrics = _metricsStore.Read();
        }
        catch (IOException)
        {
            _metrics = CatInteractionMetrics.Empty;
        }
        catch (UnauthorizedAccessException)
        {
            _metrics = CatInteractionMetrics.Empty;
        }
    }

    private void RefreshHabitProfile()
    {
        _behavior.HabitProfile = CatHabitProfile.FromEvents(_eventStore.ReadAll());
    }

    private void SaveLearningState()
    {
        _learningStateStore.Write(new CatLearningState(_learningFeedback.SeenKeys.OrderBy(key => key).ToArray()));
    }

    private void CountMetric(Func<CatInteractionMetrics, CatInteractionMetrics> count)
    {
        try
        {
            _metrics = count(_metrics);
            _metricsStore.Write(_metrics);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void SetQuietMode(bool enabled)
    {
        var wasQuiet = _behavior.QuietMode;
        Apply(_behavior.SetQuietMode(enabled, DateTimeOffset.Now));
        if (_quietModeMenuItem is not null)
        {
            _quietModeMenuItem.Header = enabled ? "恢复陪伴" : "安静一会儿";
        }

        if (enabled && !wasQuiet)
        {
            CountMetric(metrics => metrics.CountQuietModeEnable());
        }

        _trayIcon.SetQuietMode(enabled);
        ShowFeedback(enabled ? "安静陪着你" : "回来陪你");
    }

    private static string GetEventStorePath()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localData, "MyCat", "events.json");
    }

    private static string GetLearningStateStorePath()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localData, "MyCat", "learning-state.json");
    }

    private static string GetMetricsStorePath()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localData, "MyCat", "interaction-metrics.json");
    }

    private void PositionNearWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        Left = Math.Max(workArea.Left + SafeMargin, workArea.Right - Width - 112);
        Top = Math.Max(workArea.Top + SafeMargin, workArea.Bottom - Height - SafeMargin);
    }

    private void RememberPlacement()
    {
        _preferredPlacement = new Point(Left, Top);
        _preferredPlacementUntil = DateTimeOffset.Now + DragPlacementMemory;
        _walkTarget = _preferredPlacement.Value;
        _walkPurpose = WalkPurpose.Placed;
    }

    private bool HasPassedDragThreshold(Point pointer)
    {
        return Math.Abs(pointer.X - _dragStartScreen.X) >= SystemParameters.MinimumHorizontalDragDistance
            || Math.Abs(pointer.Y - _dragStartScreen.Y) >= SystemParameters.MinimumVerticalDragDistance;
    }

    private Point GetPointerScreenPosition(MouseEventArgs e)
    {
        return FromDevicePoint(PointToScreen(e.GetPosition(this)));
    }

    private Point FromDevicePoint(Point point)
    {
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget?.TransformFromDevice.Transform(point) ?? point;
    }

    private Point ClampToSafePosition(Point candidate)
    {
        var safe = GetSafePositionBounds();
        return new Point(
            Math.Clamp(candidate.X, safe.Left, safe.Right),
            Math.Clamp(candidate.Y, safe.Top, safe.Bottom));
    }

    private Rect GetSafePositionBounds()
    {
        var workArea = SystemParameters.WorkArea;
        var maximumLeft = Math.Max(workArea.Left + SafeMargin, workArea.Right - Width - SafeMargin);
        var maximumTop = Math.Max(workArea.Top + SafeMargin, workArea.Bottom - Height - SafeMargin);
        return new Rect(
            workArea.Left + SafeMargin,
            workArea.Top + SafeMargin,
            maximumLeft - workArea.Left - SafeMargin,
            maximumTop - workArea.Top - SafeMargin);
    }

    private void PickWalkTarget(DateTimeOffset now)
    {
        if (!_behavior.QuietMode && now >= _nextWindowInterestAt)
        {
            if (TryPickWindowTarget(out var windowTarget))
            {
                _walkTarget = windowTarget;
                _walkPurpose = WalkPurpose.Window;
                _nextWindowInterestAt = now + WindowVisitCooldown;
                return;
            }

            _nextWindowInterestAt = now + WindowVisitRetry;
        }

        if (_preferredPlacement is { } placement && now < _preferredPlacementUntil)
        {
            _walkTarget = PickNear(placement, 118);
            _walkPurpose = WalkPurpose.Placed;
            return;
        }

        _walkTarget = PickWideRoamTarget();
        _walkPurpose = WalkPurpose.Roam;
    }

    private Point PickNear(Point anchor, double radius)
    {
        return ClampToSafePosition(new Point(
            anchor.X + NextOffset(radius),
            anchor.Y + NextOffset(radius * 0.55)));
    }

    private Point PickWideRoamTarget()
    {
        var safe = GetSafePositionBounds();
        var current = new Point(Left, Top);
        var minimumTravel = Math.Max(160, Math.Min(safe.Width, safe.Height) * 0.38);
        var fallback = current;

        for (var attempt = 0; attempt < 8; attempt++)
        {
            fallback = new Point(
                NextBetween(safe.Left, safe.Right),
                NextBetween(safe.Top, safe.Bottom));
            if (Distance(current, fallback) >= minimumTravel)
            {
                break;
            }
        }

        return ClampToSafePosition(fallback);
    }

    private bool MoveTowardWalkTarget()
    {
        var current = new Point(Left, Top);
        var deltaX = _walkTarget.X - current.X;
        var deltaY = _walkTarget.Y - current.Y;
        var distance = Distance(current, _walkTarget);

        if (distance <= WalkStep)
        {
            Left = _walkTarget.X;
            Top = _walkTarget.Y;
            return true;
        }

        _sprite.FacingLeft = deltaX < 0;
        var candidate = ClampToSafePosition(new Point(
            current.X + ((deltaX / distance) * WalkStep),
            current.Y + ((deltaY / distance) * WalkStep)));
        if (candidate == current)
        {
            return true;
        }

        Left = candidate.X;
        Top = candidate.Y;
        return false;
    }

    private bool TryPickWindowTarget(out Point target)
    {
        target = default;
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero
            || foreground == new WindowInteropHelper(this).Handle
            || NativeMethods.IsIconic(foreground)
            || !NativeMethods.GetWindowRect(foreground, out var nativeRect))
        {
            return false;
        }

        var topLeft = FromDevicePoint(new Point(nativeRect.Left, nativeRect.Top));
        var bottomRight = FromDevicePoint(new Point(nativeRect.Right, nativeRect.Bottom));
        var windowRect = new Rect(topLeft, bottomRight);
        if (windowRect.Width < Width || windowRect.Height < Height / 2)
        {
            return false;
        }

        var safe = GetSafePositionBounds();
        var candidates = new[]
        {
            new Point(windowRect.Right + 10, windowRect.Bottom - Height),
            new Point(windowRect.Left - Width - 10, windowRect.Bottom - Height),
            new Point(windowRect.Right - Width - 28, windowRect.Bottom - Height + 22)
        };

        foreach (var candidate in candidates)
        {
            var clamped = ClampToSafePosition(candidate);
            if (safe.Contains(clamped) && Distance(new Point(Left, Top), clamped) > 24)
            {
                target = clamped;
                return true;
            }
        }

        return false;
    }

    private double NextOffset(double radius)
    {
        return (_random.NextDouble() * radius * 2) - radius;
    }

    private double NextBetween(double minimum, double maximum)
    {
        return maximum <= minimum
            ? minimum
            : minimum + (_random.NextDouble() * (maximum - minimum));
    }

    private static double Distance(Point first, Point second)
    {
        var x = second.X - first.X;
        var y = second.Y - first.Y;
        return Math.Sqrt((x * x) + (y * y));
    }

    private enum WalkPurpose
    {
        Roam,
        Placed,
        Window
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
