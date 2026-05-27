using System.Diagnostics;
using System.IO;
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
    private const double DragStartThreshold = 8;
    private const double DragHeadAnchorY = 28;
    private const double CatFootAnchorInset = 8;
    private const double EdgeSnapInset = 4;
    private const double WindowEdgeSnapDistance = 48;
    private const double TaskbarEdgeSnapDistance = 56;
    private const double TaskbarLeftGuard = 130;
    private const double TaskbarRightGuard = 210;
    private const double MouseTrackDeadZone = 18;
    private const double MouseTrackInitialDeadZone = 3;
    private const double WindowEdgeAwarenessDistance = 54;
    private const double TaskbarMouseExitDistance = 72;
    private static readonly TimeSpan DragPlacementMemory = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan MouseNoticeCooldown = TimeSpan.FromSeconds(18);
    private static readonly TimeSpan WindowVisitCooldown = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan WindowVisitRetry = TimeSpan.FromSeconds(40);
    private static readonly TimeSpan WindowAvoidCooldown = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan TaskbarVisitMinimumCooldown = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TaskbarVisitRandomCooldown = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan IdleMouseGlanceMinimumDelay = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan IdleMouseGlanceRandomDelay = TimeSpan.FromSeconds(16);
    private static readonly TimeSpan IdleMouseGlanceDuration = TimeSpan.FromSeconds(3);
    private readonly CatAnimationPlayer _animationPlayer;
    private readonly CatBehaviorController _behavior = new();
    private readonly ContextMenu _catMenu;
    private readonly DesktopEnvironmentService _environment = new();
    private readonly JsonCatEventStore _eventStore = new(AppPaths.EventStorePath);
    private readonly JsonCatLearningStateStore _learningStateStore = new(AppPaths.LearningStateStorePath);
    private readonly JsonCatInteractionMetricsStore _metricsStore = new(AppPaths.MetricsStorePath);
    private readonly JsonCatBehaviorSettingsStore _behaviorSettingsStore = new(AppPaths.BehaviorSettingsStorePath);
    private readonly Border _feedbackBubble;
    private readonly TextBlock _feedbackText = new();
    private readonly Random _random = new();
    private readonly CatSprite _sprite = new();
    private readonly DispatcherTimer _feedbackTimer;
    private readonly DispatcherTimer _tickTimer;
    private readonly TrayIconHost _trayIcon;
    private CatLearningFeedbackTracker _learningFeedback = new();
    private CatBehaviorSettings _behaviorSettings = CatBehaviorSettings.Empty;
    private CatInteractionMetrics _metrics = CatInteractionMetrics.Empty;
    private BehaviorSettingsWindow? _behaviorSettingsWindow;
    private MenuItem? _quietModeMenuItem;
    private Point _dragPointerOffset;
    private Point _dragStartScreen;
    private Point _walkTarget;
    private Point? _preferredPlacement;
    private DateTimeOffset _preferredPlacementUntil;
    private DateTimeOffset _nextMouseNoticeAt;
    private DateTimeOffset _nextWindowInterestAt;
    private DateTimeOffset _nextWindowAvoidAt;
    private DateTimeOffset _nextTaskbarInterestAt;
    private DateTimeOffset _nextIdleMouseGlanceAt = DateTimeOffset.MaxValue;
    private DateTimeOffset _dragHoldAt;
    private DateTimeOffset? _idleMouseGlanceEndsAt;
    private DesktopWindowSnapshot? _lastForegroundWindow;
    private CatActionId _mouseTrackActionId = CatActionId.MouseTrackRight;
    private bool _pointerDown;
    private bool _dragging;
    private bool _pendingMouseTrackAfterPet;
    private bool _idleMouseGlanceActive;
    private bool _walkEndMouseGlancePending;
    private bool _currentIdleMouseGlanceFromWalkEnd;
    private CatState? _lastAppliedState;
    private WalkPurpose _walkPurpose = WalkPurpose.Roam;

    public DesktopCatWindow()
    {
        Title = ProductInfo.Name;
        Width = 184;
        Height = 168;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;

        _animationPlayer = new CatAnimationPlayer(CreateAnimationCatalog(), _sprite);
        _catMenu = CreateCatMenu();
        _sprite.ContextMenu = _catMenu;
        _feedbackBubble = CreateFeedbackBubble();
        Content = CreateCatSurface();
        _trayIcon = new TrayIconHost(
            eventType => Dispatcher.BeginInvoke(() => Record(eventType, CatEventSource.TrayMenu)),
            enabled => Dispatcher.BeginInvoke(() => SetQuietMode(enabled)),
            () => Dispatcher.BeginInvoke(OpenBehaviorSettings),
            () => Dispatcher.BeginInvoke(ShowAbout),
            () => Dispatcher.BeginInvoke(() => OpenDirectory(AppPaths.DataDirectory, "数据目录")),
            () => Dispatcher.BeginInvoke(() => OpenDirectory(AppPaths.LogDirectory, "日志目录")),
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

    private static CatAnimationCatalog CreateAnimationCatalog()
    {
        try
        {
            return new CatAnimationCatalog();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            AppLogger.LogException("AssetCatalogLoadFailed", ex);
            throw;
        }
    }

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        AppLogger.Log("WindowLoaded");
        LoadBehaviorSettings();
        LoadLearningState();
        LoadMetrics();
        PositionNearWorkArea();
        var now = DateTimeOffset.Now;
        _walkTarget = new Point(Left, Top);
        _nextMouseNoticeAt = now + TimeSpan.FromSeconds(4);
        _nextWindowInterestAt = now + TimeSpan.FromSeconds(45);
        _nextTaskbarInterestAt = now + TimeSpan.FromMinutes(5);
        Apply(_behavior.Start(now));
        _tickTimer.Start();
    }

    private void HandleClosed(object? sender, EventArgs e)
    {
        AppLogger.Log("WindowClosed");
        _tickTimer.Stop();
        _feedbackTimer.Stop();
        _animationPlayer.Dispose();
        _trayIcon.Dispose();
    }

    private void HandleCatPointerDown(object sender, MouseButtonEventArgs e)
    {
        _catMenu.IsOpen = false;
        _pendingMouseTrackAfterPet = false;
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

        var now = DateTimeOffset.Now;
        if (!_dragging)
        {
            _dragging = true;
            _pendingMouseTrackAfterPet = false;
            _dragPointerOffset = new Point(Width / 2, DragHeadAnchorY);
            var lift = _behavior.DragLifted(now);
            _dragHoldAt = lift.EndsAt;
            Apply(lift);
            CountMetric(metrics => metrics.CountDragLift());
        }
        else if (_behavior.Current?.State is CatState.DragLift && now >= _dragHoldAt)
        {
            Apply(_behavior.DragHeld(now));
        }

        _sprite.Cursor = Cursors.SizeAll;
        var candidate = ClampToDragPosition(new Point(
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
            SnapPlacementToEdge();
            RememberPlacement();
            Apply(_behavior.DragDropped(DateTimeOffset.Now));
            CountMetric(metrics => metrics.CountDrag());
            CountMetric(metrics => metrics.CountDragDrop());
            ShowFeedback("就待这儿");
            e.Handled = true;
            return;
        }

        _pendingMouseTrackAfterPet = true;
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
        if (_behavior.Current?.State is CatState.TaskbarVisit && IsMouseNearTaskbar())
        {
            _nextTaskbarInterestAt = NextTaskbarInterestAt(now);
            Apply(_behavior.Start(now));
            return;
        }

        if (_pendingMouseTrackAfterPet
            && _behavior.Current?.State is CatState.PetReact
            && now >= _behavior.Current.EndsAt)
        {
            _pendingMouseTrackAfterPet = false;
            _mouseTrackActionId = PickMouseTrackActionId(_mouseTrackActionId, MouseTrackInitialDeadZone);
            Apply(_behavior.TrackMouse(now, _mouseTrackActionId));
            CountMetric(metrics => metrics.CountMouseTrack());
            return;
        }

        if (_behavior.Current?.State is CatState.MouseTrack)
        {
            UpdateMouseTrackDirection();
        }

        if (HandleIdleMouseGlance(now))
        {
            return;
        }

        if (_behavior.Current?.State is CatState.WindowAvoid && MoveTowardWalkTarget())
        {
            Apply(_behavior.Advance(_behavior.Current.EndsAt)!);
            return;
        }

        if (_behavior.Current?.State is CatState.Walk && MoveTowardWalkTarget())
        {
            SettleAfterWalk(now);
            return;
        }

        if (TryStartWindowAvoid(now))
        {
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
        var previousState = _lastAppliedState;
        var isWalkEndIdle = transition.State is CatState.Idle
            && previousState is CatState.Walk or CatState.EdgePause;

        if (transition.State is CatState.Walk)
        {
            _walkEndMouseGlancePending = false;
            _currentIdleMouseGlanceFromWalkEnd = false;
        }
        else if (isWalkEndIdle)
        {
            _walkEndMouseGlancePending = true;
            _currentIdleMouseGlanceFromWalkEnd = false;
        }
        else if (transition.State is not CatState.Idle)
        {
            _walkEndMouseGlancePending = false;
            _currentIdleMouseGlanceFromWalkEnd = false;
        }

        if (transition.State is CatState.Idle)
        {
            ScheduleIdleMouseGlance(transition.StartedAt);
        }
        else
        {
            ClearIdleMouseGlance();
        }

        if (transition.State is CatState.Walk)
        {
            PickWalkTarget(transition.StartedAt);
        }

        if (transition.State is not CatState.MouseTrack && _walkTarget.X != Left)
        {
            _sprite.FacingLeft = _walkTarget.X < Left;
        }

        _animationPlayer.Play(transition.ActionId);
        _lastAppliedState = transition.State;
    }

    private bool HandleIdleMouseGlance(DateTimeOffset now)
    {
        if (_behavior.Current?.State is not CatState.Idle || _behavior.QuietMode)
        {
            ClearIdleMouseGlance();
            return false;
        }

        if (_idleMouseGlanceActive)
        {
            if (_idleMouseGlanceEndsAt is not null && now >= _idleMouseGlanceEndsAt)
            {
                EndIdleMouseGlance(now);
                return false;
            }

            UpdateIdleMouseGlanceDirection();
            return true;
        }

        if (now < _nextIdleMouseGlanceAt)
        {
            return false;
        }

        StartIdleMouseGlance(now);
        return true;
    }

    private void StartIdleMouseGlance(DateTimeOffset now)
    {
        _idleMouseGlanceActive = true;
        _idleMouseGlanceEndsAt = now + IdleMouseGlanceDuration;
        _currentIdleMouseGlanceFromWalkEnd = _walkEndMouseGlancePending;
        _walkEndMouseGlancePending = false;
        _mouseTrackActionId = PickMouseTrackActionId(_mouseTrackActionId, MouseTrackInitialDeadZone);
        _animationPlayer.Play(_mouseTrackActionId);
        CountMetric(metrics => metrics.CountMouseTrack());
    }

    private void EndIdleMouseGlance(DateTimeOffset now)
    {
        _idleMouseGlanceActive = false;
        _idleMouseGlanceEndsAt = null;
        _animationPlayer.Play(CatActionId.IdleSit);
        if (_currentIdleMouseGlanceFromWalkEnd)
        {
            _currentIdleMouseGlanceFromWalkEnd = false;
            _nextIdleMouseGlanceAt = DateTimeOffset.MaxValue;
            return;
        }

        ScheduleIdleMouseGlance(now);
    }

    private void UpdateIdleMouseGlanceDirection()
    {
        var next = PickMouseTrackActionId(_mouseTrackActionId, MouseTrackDeadZone);
        if (next == _mouseTrackActionId)
        {
            return;
        }

        _mouseTrackActionId = next;
        _animationPlayer.Play(next);
    }

    private void ScheduleIdleMouseGlance(DateTimeOffset now)
    {
        _idleMouseGlanceActive = false;
        _idleMouseGlanceEndsAt = null;
        _nextIdleMouseGlanceAt = _behavior.QuietMode
            ? DateTimeOffset.MaxValue
            : now + IdleMouseGlanceMinimumDelay + TimeSpan.FromTicks((long)(_random.NextDouble() * IdleMouseGlanceRandomDelay.Ticks));
    }

    private void ClearIdleMouseGlance()
    {
        _idleMouseGlanceActive = false;
        _idleMouseGlanceEndsAt = null;
        _nextIdleMouseGlanceAt = DateTimeOffset.MaxValue;
    }

    private void SettleAfterWalk(DateTimeOffset now)
    {
        if (_walkPurpose is WalkPurpose.Taskbar)
        {
            var taskbarVisit = _behavior.VisitTaskbar(now, lie: _random.NextDouble() < 0.45);
            _nextTaskbarInterestAt = NextTaskbarInterestAt(now);
            if (taskbarVisit is not null)
            {
                CountMetric(metrics => metrics.CountTaskbarVisit());
                Apply(taskbarVisit);
                return;
            }
        }

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
        menu.Items.Add(MenuItem("调整作息", OpenBehaviorSettings));
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

    private void OpenBehaviorSettings()
    {
        try
        {
            if (_behaviorSettingsWindow is { IsVisible: true })
            {
                _behaviorSettingsWindow.Activate();
                return;
            }

            RefreshHabitProfile();
            _behaviorSettingsWindow = new BehaviorSettingsWindow(
                _behavior.HabitProfile,
                _behaviorSettings,
                _behavior.QuietMode,
                SaveBehaviorSettings)
            {
                Owner = this
            };
            _behaviorSettingsWindow.Closed += (_, _) => _behaviorSettingsWindow = null;
            _behaviorSettingsWindow.Show();
        }
        catch (Exception ex)
        {
            AppLogger.LogException("BehaviorSettingsOpenFailed", ex);
            _behaviorSettingsWindow = null;
            System.Windows.MessageBox.Show(
                this,
                $"调整作息暂时没有打开成功：{ex.Message}",
                "My Cat",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
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
        catch (IOException ex)
        {
            AppLogger.LogException("RecordSaveFailed", ex);
            ShowFeedback("这次没记上");
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLogger.LogException("RecordSaveFailed", ex);
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
        try
        {
            RefreshHabitProfile();
            _learningFeedback = new CatLearningFeedbackTracker(_learningStateStore.Read().SeenFeedbackKeys);
        }
        catch (IOException ex)
        {
            AppLogger.LogException("LearningStateLoadFailed", ex);
            _learningFeedback = new CatLearningFeedbackTracker();
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLogger.LogException("LearningStateLoadFailed", ex);
            _learningFeedback = new CatLearningFeedbackTracker();
        }
    }

    private void LoadBehaviorSettings()
    {
        try
        {
            _behaviorSettings = _behaviorSettingsStore.Read();
            _behavior.BehaviorSettings = _behaviorSettings;
        }
        catch (IOException ex)
        {
            AppLogger.LogException("BehaviorSettingsLoadFailed", ex);
            _behaviorSettings = CatBehaviorSettings.Empty;
            _behavior.BehaviorSettings = _behaviorSettings;
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLogger.LogException("BehaviorSettingsLoadFailed", ex);
            _behaviorSettings = CatBehaviorSettings.Empty;
            _behavior.BehaviorSettings = _behaviorSettings;
        }
    }

    private void LoadMetrics()
    {
        try
        {
            _metrics = _metricsStore.Read();
        }
        catch (IOException ex)
        {
            AppLogger.LogException("MetricsLoadFailed", ex);
            _metrics = CatInteractionMetrics.Empty;
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLogger.LogException("MetricsLoadFailed", ex);
            _metrics = CatInteractionMetrics.Empty;
        }
    }

    private void RefreshHabitProfile()
    {
        _behavior.HabitProfile = CatHabitProfile.FromEvents(_eventStore.ReadAll());
        _behavior.BehaviorSettings = _behaviorSettings;
    }

    private void SaveBehaviorSettings(CatBehaviorSettings settings)
    {
        try
        {
            _behaviorSettingsStore.Write(settings);
        }
        catch (IOException ex)
        {
            AppLogger.LogException("BehaviorSettingsSaveFailed", ex);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLogger.LogException("BehaviorSettingsSaveFailed", ex);
            throw;
        }

        _behaviorSettings = settings;
        _behavior.BehaviorSettings = settings;
        ShowFeedback("作息已更新");
    }

    private void SaveLearningState()
    {
        try
        {
            _learningStateStore.Write(new CatLearningState(_learningFeedback.SeenKeys.OrderBy(key => key).ToArray()));
        }
        catch (IOException ex)
        {
            AppLogger.LogException("LearningStateSaveFailed", ex);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLogger.LogException("LearningStateSaveFailed", ex);
            throw;
        }
    }

    private void CountMetric(Func<CatInteractionMetrics, CatInteractionMetrics> count)
    {
        try
        {
            _metrics = count(_metrics);
            _metricsStore.Write(_metrics);
        }
        catch (IOException ex)
        {
            AppLogger.LogException("MetricsSaveFailed", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLogger.LogException("MetricsSaveFailed", ex);
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

    private void ShowAbout()
    {
        System.Windows.MessageBox.Show(
            this,
            $"{ProductInfo.Name} {ProductInfo.Version}{Environment.NewLine}{Environment.NewLine}" +
            $"数据目录：{AppPaths.DataDirectory}{Environment.NewLine}" +
            $"日志目录：{AppPaths.LogDirectory}",
            $"关于 {ProductInfo.Name}",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OpenDirectory(string path, string label)
    {
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            AppLogger.LogException($"{label}OpenFailed", ex);
            ShowFeedback($"{label}没有打开");
        }
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
        return Math.Abs(pointer.X - _dragStartScreen.X) >= DragStartThreshold
            || Math.Abs(pointer.Y - _dragStartScreen.Y) >= DragStartThreshold;
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

    private Point ClampToDragPosition(Point candidate)
    {
        var screen = GetFullScreenPositionBounds();
        return new Point(
            Math.Clamp(candidate.X, screen.Left, screen.Right),
            Math.Clamp(candidate.Y, screen.Top, screen.Bottom));
    }

    private Point ClampWalkPosition(Point candidate)
    {
        return _walkPurpose is WalkPurpose.Taskbar or WalkPurpose.WindowAvoid
            ? ClampToDragPosition(candidate)
            : ClampToSafePosition(candidate);
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

    private Rect GetFullScreenPositionBounds()
    {
        var screen = _environment.FullScreenBounds;
        var maximumLeft = Math.Max(screen.Left, screen.Right - Width);
        var maximumTop = Math.Max(screen.Top, screen.Bottom - Height);
        return new Rect(
            screen.Left,
            screen.Top,
            maximumLeft - screen.Left,
            maximumTop - screen.Top);
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

        if (!_behavior.QuietMode && now >= _nextTaskbarInterestAt && TryPickTaskbarTarget(out var taskbarTarget))
        {
            _walkTarget = taskbarTarget;
            _walkPurpose = WalkPurpose.Taskbar;
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
        var candidate = ClampWalkPosition(new Point(
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
        var foreground = _environment.GetForegroundWindowSnapshot(new WindowInteropHelper(this).Handle, FromDevicePoint);
        if (foreground is null)
        {
            return false;
        }

        var windowRect = foreground.Value.Rect;
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

    private bool TryStartWindowAvoid(DateTimeOffset now)
    {
        var foreground = _environment.GetForegroundWindowSnapshot(new WindowInteropHelper(this).Handle, FromDevicePoint);
        if (foreground is null)
        {
            _lastForegroundWindow = null;
            return false;
        }

        var previous = _lastForegroundWindow;
        _lastForegroundWindow = foreground;
        if (now < _nextWindowAvoidAt
            || previous is null
            || previous.Value.Handle != foreground.Value.Handle
            || !HasWindowMoved(previous.Value.Rect, foreground.Value.Rect)
            || !IsCatNearWindowEdge(foreground.Value.Rect))
        {
            return false;
        }

        var startle = _behavior.StartleFromWindow(now);
        if (startle is null)
        {
            return false;
        }

        _walkTarget = PickWindowAvoidTarget(foreground.Value.Rect);
        _walkPurpose = WalkPurpose.WindowAvoid;
        _nextWindowAvoidAt = now + WindowAvoidCooldown;
        CountMetric(metrics => metrics.CountWindowAvoid());
        Apply(startle);
        return true;
    }

    private bool TryPickTaskbarTarget(out Point target)
    {
        target = default;
        var taskbar = _environment.BottomTaskbarBounds;
        if (taskbar is null)
        {
            return false;
        }

        var x = NextBetween(taskbar.Value.Left + TaskbarLeftGuard, taskbar.Value.Right - TaskbarRightGuard - Width);
        var y = taskbar.Value.Top - Height + 8;
        target = ClampToDragPosition(new Point(x, y));
        return true;
    }

    private void SnapPlacementToEdge()
    {
        var current = new Point(Left, Top);
        if (TrySnapToForegroundWindow(current, out var windowSnap)
            || TrySnapToBottomTaskbar(current, out windowSnap))
        {
            Left = windowSnap.X;
            Top = windowSnap.Y;
        }
    }

    private bool TrySnapToForegroundWindow(Point current, out Point snapped)
    {
        snapped = default;
        var foreground = _environment.GetForegroundWindowSnapshot(new WindowInteropHelper(this).Handle, FromDevicePoint);
        if (foreground is null)
        {
            return false;
        }

        var windowRect = foreground.Value.Rect;
        if (windowRect.Width < Width / 2 || windowRect.Height < Height / 3)
        {
            return false;
        }

        var foot = CatFootPoint(current);
        var candidates = new List<EdgeSnapCandidate>(4);
        AddHorizontalWindowSnap(candidates, foot, windowRect.Top, windowRect.Left, windowRect.Right);
        AddHorizontalWindowSnap(candidates, foot, windowRect.Bottom, windowRect.Left, windowRect.Right);
        AddVerticalWindowSnap(candidates, foot, windowRect.Left, windowRect.Top, windowRect.Bottom);
        AddVerticalWindowSnap(candidates, foot, windowRect.Right, windowRect.Top, windowRect.Bottom);

        if (candidates.Count == 0)
        {
            return false;
        }

        snapped = ClampToDragPosition(candidates.OrderBy(candidate => candidate.Distance).First().Position);
        return true;
    }

    private bool TrySnapToBottomTaskbar(Point current, out Point snapped)
    {
        snapped = default;
        var taskbar = _environment.BottomTaskbarBounds;
        if (taskbar is null)
        {
            return false;
        }

        var foot = CatFootPoint(current);
        if (Math.Abs(foot.Y - taskbar.Value.Top) > TaskbarEdgeSnapDistance
            || foot.X < taskbar.Value.Left
            || foot.X > taskbar.Value.Right)
        {
            return false;
        }

        var x = current.X;
        var protectedLeft = taskbar.Value.Left + TaskbarLeftGuard;
        var protectedRight = taskbar.Value.Right - TaskbarRightGuard;
        if (x >= protectedLeft && x + Width <= protectedRight)
        {
            x = Math.Clamp(x, protectedLeft, protectedRight - Width);
        }

        snapped = ClampToDragPosition(new Point(x, taskbar.Value.Top - EdgeSnapInset - CatFootAnchorYOffset));
        return true;
    }

    private void AddHorizontalWindowSnap(List<EdgeSnapCandidate> candidates, Point foot, double edgeY, double left, double right)
    {
        var distance = Math.Abs(foot.Y - edgeY);
        if (distance > WindowEdgeSnapDistance || foot.X < left - WindowEdgeSnapDistance || foot.X > right + WindowEdgeSnapDistance)
        {
            return;
        }

        var x = Math.Clamp(foot.X - (Width / 2), left - (Width / 2), right - (Width / 2));
        candidates.Add(new EdgeSnapCandidate(
            distance,
            new Point(x, edgeY - EdgeSnapInset - CatFootAnchorYOffset)));
    }

    private void AddVerticalWindowSnap(List<EdgeSnapCandidate> candidates, Point foot, double edgeX, double top, double bottom)
    {
        var distance = Math.Abs(foot.X - edgeX);
        if (distance > WindowEdgeSnapDistance || foot.Y < top - WindowEdgeSnapDistance || foot.Y > bottom + WindowEdgeSnapDistance)
        {
            return;
        }

        var y = Math.Clamp(foot.Y - CatFootAnchorYOffset, top - CatFootAnchorYOffset, bottom - CatFootAnchorYOffset);
        candidates.Add(new EdgeSnapCandidate(
            distance,
            new Point(edgeX - (Width / 2), y)));
    }

    private Point CatFootPoint(Point catTopLeft)
    {
        return new Point(catTopLeft.X + (Width / 2), catTopLeft.Y + CatFootAnchorYOffset);
    }

    private double CatFootAnchorYOffset => Height - CatFootAnchorInset;

    private bool IsMouseNearTaskbar()
    {
        var taskbar = _environment.BottomTaskbarBounds;
        if (taskbar is null)
        {
            return false;
        }

        var mouse = FromDevicePoint(_environment.MouseScreenPosition);
        var inflated = taskbar.Value;
        inflated.Inflate(TaskbarMouseExitDistance, TaskbarMouseExitDistance);
        return inflated.Contains(mouse);
    }

    private DateTimeOffset NextTaskbarInterestAt(DateTimeOffset now)
    {
        return now + TaskbarVisitMinimumCooldown + TimeSpan.FromTicks((long)(_random.NextDouble() * TaskbarVisitRandomCooldown.Ticks));
    }

    private void UpdateMouseTrackDirection()
    {
        var next = PickMouseTrackActionId(_mouseTrackActionId, MouseTrackDeadZone);
        if (next == _mouseTrackActionId)
        {
            return;
        }

        _mouseTrackActionId = next;
        Apply(_behavior.RetargetMouse(next));
    }

    private CatActionId PickMouseTrackActionId(CatActionId fallback, double deadZone)
    {
        var mouse = FromDevicePoint(_environment.MouseScreenPosition);
        var center = new Point(Left + (Width / 2), Top + (Height / 2));
        var dx = mouse.X - center.X;
        var dy = mouse.Y - center.Y;
        if (Math.Abs(dx) < deadZone && Math.Abs(dy) < deadZone)
        {
            return fallback;
        }

        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            _sprite.FacingLeft = dx < 0;
            return dx < 0 ? CatActionId.MouseTrackLeft : CatActionId.MouseTrackRight;
        }

        return dy < 0 ? CatActionId.MouseTrackUp : CatActionId.MouseTrackDown;
    }

    private Point PickWindowAvoidTarget(Rect windowRect)
    {
        var cat = new Rect(Left, Top, Width, Height);
        var windowCenter = new Point(windowRect.Left + (windowRect.Width / 2), windowRect.Top + (windowRect.Height / 2));
        var catCenter = new Point(cat.Left + (cat.Width / 2), cat.Top + (cat.Height / 2));
        var direction = catCenter.X < windowCenter.X ? -1 : 1;
        return ClampToSafePosition(new Point(Left + (direction * 150), Top + NextOffset(26)));
    }

    private bool IsCatNearWindowEdge(Rect windowRect)
    {
        var cat = new Rect(Left, Top, Width, Height);
        var nearWindow = windowRect;
        nearWindow.Inflate(WindowEdgeAwarenessDistance, WindowEdgeAwarenessDistance);
        if (!nearWindow.IntersectsWith(cat))
        {
            return false;
        }

        var insideWindow = windowRect;
        insideWindow.Inflate(-WindowEdgeAwarenessDistance, -WindowEdgeAwarenessDistance);
        return !insideWindow.Contains(cat);
    }

    private static bool HasWindowMoved(Rect previous, Rect current)
    {
        return Math.Abs(previous.Left - current.Left) >= 4
            || Math.Abs(previous.Top - current.Top) >= 4
            || Math.Abs(previous.Width - current.Width) >= 4
            || Math.Abs(previous.Height - current.Height) >= 4;
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
        Window,
        WindowAvoid,
        Taskbar
    }

    private readonly record struct EdgeSnapCandidate(double Distance, Point Position);
}
