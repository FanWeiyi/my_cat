using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MyCat.CatAssets;
using MyCat.CatCore;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace MyCat.WindowsShell;

internal sealed class DesktopCatWindow : Window
{
    private const double SafeMargin = 18;
    private readonly CatAnimationPlayer _animationPlayer;
    private readonly CatBehaviorController _behavior = new();
    private readonly ContextMenu _catMenu;
    private readonly JsonCatEventStore _eventStore = new(GetEventStorePath());
    private readonly Border _feedbackBubble;
    private readonly TextBlock _feedbackText = new();
    private readonly CatSprite _sprite = new();
    private readonly DispatcherTimer _feedbackTimer;
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

        _animationPlayer = new CatAnimationPlayer(new CatAnimationCatalog(), _sprite);
        _catMenu = CreateCatMenu();
        _sprite.ContextMenu = _catMenu;
        _feedbackBubble = CreateFeedbackBubble();
        Content = CreateCatSurface();
        _trayIcon = new TrayIconHost(
            eventType => Dispatcher.BeginInvoke(() => Record(eventType, CatEventSource.TrayMenu)),
            () => Dispatcher.Invoke(Close));
        _feedbackTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1.4)
        };
        _tickTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };

        Loaded += HandleLoaded;
        Closed += HandleClosed;
        _feedbackTimer.Tick += HandleFeedbackElapsed;
        _tickTimer.Tick += HandleTick;
        _sprite.MouseLeftButtonUp += HandleCatClick;
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
        _feedbackTimer.Stop();
        _animationPlayer.Dispose();
        _trayIcon.Dispose();
    }

    private void HandleCatClick(object sender, MouseButtonEventArgs e)
    {
        Apply(_behavior.Pet(DateTimeOffset.Now));
        OpenCatMenu();
        e.Handled = true;
    }

    private void HandleFeedbackElapsed(object? sender, EventArgs e)
    {
        _feedbackTimer.Stop();
        _feedbackBubble.Visibility = Visibility.Collapsed;
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
            HasDropShadow = true
        };

        menu.Items.Add(MenuItem("摸摸它", () => Apply(_behavior.Pet(DateTimeOffset.Now))));

        var tellMenu = new MenuItem { Header = "告诉它" };
        tellMenu.Items.Add(MenuItem("我家猫在睡觉", () => Record(CatEventType.Rest, CatEventSource.DesktopCatMenu)));
        tellMenu.Items.Add(MenuItem("我家猫在玩", () => Record(CatEventType.Activity, CatEventSource.DesktopCatMenu)));
        tellMenu.Items.Add(MenuItem("我家猫在陪我", () => Record(CatEventType.Accompany, CatEventSource.DesktopCatMenu)));
        menu.Items.Add(tellMenu);

        return menu;
    }

    private static MenuItem MenuItem(string header, Action action)
    {
        var item = new MenuItem { Header = header };
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
            Background = new SolidColorBrush(Color.FromArgb(226, 69, 54, 46)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 241, 215)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Top,
            Margin = new Thickness(0, 6, 0, 0),
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
            ShowFeedback("已记下");
        }
        catch (IOException)
        {
            ShowFeedback("没记上");
        }
        catch (UnauthorizedAccessException)
        {
            ShowFeedback("没记上");
        }
    }

    private void ShowFeedback(string text)
    {
        _feedbackText.Text = text;
        _feedbackBubble.Visibility = Visibility.Visible;
        _feedbackTimer.Stop();
        _feedbackTimer.Start();
    }

    private static string GetEventStorePath()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localData, "MyCat", "events.json");
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
