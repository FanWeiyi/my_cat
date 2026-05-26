using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using MyCat.CatCore;
using MediaColor = System.Windows.Media.Color;
using WpfBinding = System.Windows.Data.Binding;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfScrollBar = System.Windows.Controls.Primitives.ScrollBar;

namespace MyCat.WindowsShell;

internal sealed class BehaviorSettingsWindow : Window
{
    private static readonly SolidColorBrush InkBrush = CreateSolidBrush(30, 29, 35);
    private static readonly SolidColorBrush SoftInkBrush = CreateSolidBrush(91, 79, 72);
    private static readonly SolidColorBrush WarmCanvasBrush = CreateSolidBrush(244, 235, 219);
    private static readonly SolidColorBrush CardBrush = CreateSolidBrush(255, 253, 248);
    private static readonly SolidColorBrush WarmCardBrush = CreateSolidBrush(255, 241, 203);
    private static readonly SolidColorBrush AccentBrush = CreateSolidBrush(248, 206, 112);
    private static readonly SolidColorBrush PinkBrush = CreateSolidBrush(240, 111, 155);
    private static readonly SolidColorBrush BlackButtonBrush = CreateSolidBrush(31, 31, 34);
    private static readonly SolidColorBrush BorderLineBrush = CreateSolidBrush(239, 226, 206);

    private readonly CatHabitProfile _learnedProfile;
    private readonly Action<CatBehaviorSettings> _saveSettings;
    private readonly WpfComboBox _bucketPicker = new();
    private readonly TextBlock _headlineText = new();
    private readonly TextBlock _learnedText = new();
    private readonly TextBlock _currentText = new();
    private readonly TextBlock _statusText = new();
    private readonly Slider _restSlider = CreateSlider();
    private readonly Slider _activitySlider = CreateSlider();
    private readonly Slider _accompanySlider = CreateSlider();
    private readonly TextBlock _restValueText = new();
    private readonly TextBlock _activityValueText = new();
    private readonly TextBlock _accompanyValueText = new();
    private readonly DispatcherTimer _scrollChromeTimer = new() { Interval = TimeSpan.FromMilliseconds(850) };
    private CatBehaviorSettings _settings;
    private bool _updating;

    public BehaviorSettingsWindow(
        CatHabitProfile learnedProfile,
        CatBehaviorSettings settings,
        bool quietMode,
        Action<CatBehaviorSettings> saveSettings)
    {
        _learnedProfile = learnedProfile;
        _settings = settings;
        _saveSettings = saveSettings;

        Title = "行为节奏设置";
        Width = 520;
        Height = 680;
        MinWidth = 460;
        MinHeight = 540;
        SizeToContent = SizeToContent.Manual;
        ResizeMode = ResizeMode.CanResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = WarmCanvasBrush;
        Foreground = InkBrush;
        FontSize = 13;
        FontFamily = new WpfFontFamily("Microsoft YaHei UI");
        Content = CreateContent(quietMode);

        _bucketPicker.SelectionChanged += (_, _) => LoadSelectedBucket();
        _restSlider.ValueChanged += (_, _) => HandleSliderChanged(_restSlider);
        _activitySlider.ValueChanged += (_, _) => HandleSliderChanged(_activitySlider);
        _accompanySlider.ValueChanged += (_, _) => HandleSliderChanged(_accompanySlider);
        _scrollChromeTimer.Tick += (_, _) =>
        {
            _scrollChromeTimer.Stop();
            SetScrollBarsOpacity(this, 0);
        };
        Loaded += (_, _) =>
        {
            SelectCurrentBucket();
            LoadSelectedBucket();
            SetScrollBarsOpacity(this, 0);
        };
    }

    private UIElement CreateContent(bool quietMode)
    {
        var root = new Grid
        {
            Margin = new Thickness(20)
        };

        var shell = new Border
        {
            Background = CardBrush,
            BorderBrush = new SolidColorBrush(MediaColor.FromArgb(150, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(24),
            Padding = new Thickness(22),
            Effect = new DropShadowEffect
            {
                BlurRadius = 28,
                Direction = 270,
                Opacity = 0.16,
                ShadowDepth = 10,
                Color = MediaColor.FromRgb(130, 111, 92)
            }
        };
        root.Children.Add(shell);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(0, 0, 2, 0)
        };
        scroll.Resources.Add(typeof(WpfScrollBar), CreateWarmScrollBarStyle());
        scroll.ScrollChanged += (_, _) => ShowScrollChrome();
        shell.Child = scroll;

        var stack = new StackPanel
        {
            Orientation = WpfOrientation.Vertical
        };
        scroll.Content = stack;

        stack.Children.Add(CreatePillText("MY CAT RHYTHM"));
        stack.Children.Add(new TextBlock
        {
            Text = "今天想怎么陪你？",
            FontSize = 26,
            FontWeight = FontWeights.Black,
            Foreground = InkBrush,
            Margin = new Thickness(0, 10, 0, 4)
        });

        _headlineText.TextWrapping = TextWrapping.Wrap;
        _headlineText.FontWeight = FontWeights.SemiBold;
        _headlineText.Foreground = SoftInkBrush;
        _headlineText.Margin = new Thickness(0, 0, 0, 14);
        stack.Children.Add(_headlineText);

        stack.Children.Add(CreateWarmNote(quietMode
            ? "安静模式已开启，小猫会把脚步放轻一点，少一些主动打扰。"
            : "保存后会优先使用你的偏好；没有手动设置时，小猫会继续从日常陪伴里学习。"));

        foreach (var bucket in Enum.GetValues<CatTimeBucket>())
        {
            _bucketPicker.Items.Add(new ComboBoxItem
            {
                Content = BucketLabel(bucket),
                Tag = bucket
            });
        }

        _bucketPicker.Margin = new Thickness(0, 8, 0, 0);
        _bucketPicker.Padding = new Thickness(12, 8, 12, 8);
        _bucketPicker.Background = CardBrush;
        _bucketPicker.Foreground = InkBrush;
        _bucketPicker.BorderBrush = BorderLineBrush;
        _bucketPicker.BorderThickness = new Thickness(1);
        _bucketPicker.FontWeight = FontWeights.SemiBold;
        _bucketPicker.Style = CreateRoundedComboBoxStyle();
        stack.Children.Add(CreateSelectorPanel());
        stack.Children.Add(CreateInfoPanel());
        stack.Children.Add(CreateSliderRow("休息", "睡觉、趴着、安静停留", "静", _restSlider, _restValueText, WarmCardBrush));
        stack.Children.Add(CreateSliderRow("活动", "走动、任务栏活动、窗口边缘活动", "动", _activitySlider, _activityValueText, WarmCardBrush));
        stack.Children.Add(CreateSliderRow("陪伴", "坐着发呆、靠近但不打扰", "陪", _accompanySlider, _accompanyValueText, WarmCardBrush));
        stack.Children.Add(CreateButtons());

        _statusText.FontWeight = FontWeights.SemiBold;
        _statusText.Margin = new Thickness(4, 10, 4, 0);
        _statusText.TextAlignment = TextAlignment.Center;
        stack.Children.Add(_statusText);
        return root;
    }

    private Border CreateSelectorPanel()
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "选择时间段",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = SoftInkBrush
        });
        panel.Children.Add(_bucketPicker);

        return CreateCard(panel, new Thickness(0, 0, 0, 12), WarmCardBrush);
    }

    private Border CreateInfoPanel()
    {
        _learnedText.TextWrapping = TextWrapping.Wrap;
        _currentText.TextWrapping = TextWrapping.Wrap;
        _learnedText.Foreground = SoftInkBrush;
        _currentText.Foreground = InkBrush;
        _currentText.FontWeight = FontWeights.SemiBold;
        _currentText.Margin = new Thickness(0, 4, 0, 0);

        var panel = new StackPanel();
        panel.Children.Add(_learnedText);
        panel.Children.Add(_currentText);

        return CreateCard(panel, new Thickness(0, 0, 0, 12), CreateSolidBrush(255, 248, 235));
    }

    private UIElement CreateSliderRow(
        string title,
        string description,
        string badge,
        Slider slider,
        TextBlock valueText,
        WpfBrush background)
    {
        var grid = new Grid
        {
            Margin = new Thickness(0)
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });

        var badgeBlock = new Border
        {
            Width = 32,
            Height = 32,
            Background = AccentBrush,
            CornerRadius = new CornerRadius(12),
            HorizontalAlignment = WpfHorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock
            {
                Text = badge,
                FontWeight = FontWeights.Black,
                Foreground = InkBrush,
                HorizontalAlignment = WpfHorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetColumn(badgeBlock, 0);
        grid.Children.Add(badgeBlock);

        var label = new StackPanel();
        label.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 15,
            FontWeight = FontWeights.Black,
            Foreground = InkBrush
        });
        label.Children.Add(new TextBlock
        {
            Text = description,
            TextWrapping = TextWrapping.Wrap,
            Foreground = SoftInkBrush,
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 8)
        });
        label.Children.Add(slider);
        Grid.SetColumn(label, 0);
        Grid.SetColumnSpan(label, 2);
        label.Margin = new Thickness(42, 0, 8, 0);
        grid.Children.Add(label);

        valueText.VerticalAlignment = VerticalAlignment.Center;
        valueText.HorizontalAlignment = WpfHorizontalAlignment.Right;
        valueText.FontWeight = FontWeights.Black;
        valueText.Foreground = InkBrush;
        valueText.Background = WpfBrushes.White;
        valueText.Padding = new Thickness(9, 4, 9, 4);
        Grid.SetColumn(valueText, 2);
        grid.Children.Add(valueText);

        return CreateCard(grid, new Thickness(0, 0, 0, 10), background);
    }

    private UIElement CreateButtons()
    {
        var buttons = new Grid
        {
            Margin = new Thickness(0, 8, 0, 0)
        };

        for (var index = 0; index < 4; index++)
        {
            buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        AddButton(buttons, CreateButton("保存设置", SaveManual, true), 0);
        AddButton(buttons, CreateButton("恢复学习值", RestoreLearned, false), 1);
        AddButton(buttons, CreateButton("恢复默认", RestoreDefault, false), 2);
        AddButton(buttons, CreateButton("关闭", Close, false), 3);
        return buttons;
    }

    private static void AddButton(Grid grid, WpfButton button, int column)
    {
        Grid.SetColumn(button, column);
        grid.Children.Add(button);
    }

    private static WpfButton CreateButton(string text, Action action, bool primary)
    {
        var button = new WpfButton
        {
            Content = text,
            MinWidth = 0,
            Margin = new Thickness(4, 0, 4, 0),
            Padding = new Thickness(10, 9, 10, 9),
            FontWeight = FontWeights.Bold,
            Background = primary ? BlackButtonBrush : WpfBrushes.White,
            Foreground = primary ? WpfBrushes.White : InkBrush,
            BorderBrush = primary ? BlackButtonBrush : BorderLineBrush,
            BorderThickness = new Thickness(1),
            HorizontalAlignment = WpfHorizontalAlignment.Stretch,
            Style = CreateRoundedButtonStyle()
        };
        button.Click += (_, _) => action();
        return button;
    }

    private void SelectCurrentBucket()
    {
        var current = CatTimeBucketResolver.Resolve(DateTimeOffset.Now);
        foreach (ComboBoxItem item in _bucketPicker.Items)
        {
            if (item.Tag is CatTimeBucket bucket && bucket == current)
            {
                _bucketPicker.SelectedItem = item;
                return;
            }
        }

        _bucketPicker.SelectedIndex = 0;
    }

    private void LoadSelectedBucket()
    {
        var bucket = SelectedBucket();
        var learned = _learnedProfile.For(bucket);
        var current = _settings.ResolveWeights(bucket, _learnedProfile);
        SetSliders(current);
        RefreshText(bucket, learned, current);
        _statusText.Text = string.Empty;
    }

    private void SaveManual()
    {
        var bucket = SelectedBucket();
        var weights = ReadSliderWeights();
        Persist(_settings.WithManual(bucket, weights), "已保存当前时间段设置");
        SetSliders(weights);
        RefreshText(bucket, _learnedProfile.For(bucket), weights);
    }

    private void RestoreLearned()
    {
        var bucket = SelectedBucket();
        var learned = _learnedProfile.For(bucket);
        Persist(_settings.WithoutManual(bucket), "已恢复为学习值");
        SetSliders(learned);
        RefreshText(bucket, learned, learned);
    }

    private void RestoreDefault()
    {
        var bucket = SelectedBucket();
        Persist(_settings.WithManual(bucket, CatHabitWeights.Default), "已恢复系统默认");
        SetSliders(CatHabitWeights.Default);
        RefreshText(bucket, _learnedProfile.For(bucket), CatHabitWeights.Default);
    }

    private void Persist(CatBehaviorSettings settings, string message)
    {
        try
        {
            _saveSettings(settings);
            _settings = settings;
            _statusText.Text = message;
            _statusText.Foreground = new SolidColorBrush(MediaColor.FromRgb(83, 118, 68));
        }
        catch (IOException)
        {
            ShowSaveFailure();
        }
        catch (UnauthorizedAccessException)
        {
            ShowSaveFailure();
        }
    }

    private void ShowSaveFailure()
    {
        _statusText.Text = "这次没有保存成功";
        _statusText.Foreground = new SolidColorBrush(MediaColor.FromRgb(176, 68, 56));
    }

    private void RefreshText(CatTimeBucket bucket, CatHabitWeights learned, CatHabitWeights current)
    {
        var source = _settings.TryGetManual(bucket, out _)
            ? "手动设置"
            : "观察记录学习";
        _headlineText.Text = $"现在是{BucketShortLabel(CatTimeBucketResolver.Resolve(DateTimeOffset.Now))}，小猫更容易：{DominantLabel(current)}";
        _learnedText.Text = $"已学习到：{FormatWeights(learned)}";
        _currentText.Text = $"当前使用：{FormatWeights(current)}（{source}）";
        UpdateValueTexts();
    }

    private void HandleSliderChanged(Slider changed)
    {
        if (_updating)
        {
            return;
        }

        _updating = true;
        var others = new[] { _restSlider, _activitySlider, _accompanySlider }
            .Where(slider => !ReferenceEquals(slider, changed))
            .ToArray();
        var remaining = Math.Max(0, 100 - changed.Value);
        var otherTotal = others.Sum(slider => slider.Value);
        if (otherTotal <= 0)
        {
            foreach (var slider in others)
            {
                slider.Value = remaining / others.Length;
            }
        }
        else
        {
            foreach (var slider in others)
            {
                slider.Value = remaining * (slider.Value / otherTotal);
            }
        }

        _updating = false;
        UpdateValueTexts();
        var bucket = SelectedBucket();
        RefreshText(bucket, _learnedProfile.For(bucket), ReadSliderWeights());
    }

    private CatHabitWeights ReadSliderWeights()
    {
        return CatHabitWeights.Normalize(_restSlider.Value, _activitySlider.Value, _accompanySlider.Value);
    }

    private void SetSliders(CatHabitWeights weights)
    {
        _updating = true;
        var normalized = weights.Normalized();
        _restSlider.Value = normalized.RestWeight * 100;
        _activitySlider.Value = normalized.ActivityWeight * 100;
        _accompanySlider.Value = normalized.AccompanyWeight * 100;
        _updating = false;
        UpdateValueTexts();
    }

    private void UpdateValueTexts()
    {
        _restValueText.Text = $"{_restSlider.Value:0}%";
        _activityValueText.Text = $"{_activitySlider.Value:0}%";
        _accompanyValueText.Text = $"{_accompanySlider.Value:0}%";
    }

    private CatTimeBucket SelectedBucket()
    {
        return _bucketPicker.SelectedItem is ComboBoxItem { Tag: CatTimeBucket bucket }
            ? bucket
            : CatTimeBucketResolver.Resolve(DateTimeOffset.Now);
    }

    private static Slider CreateSlider()
    {
        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0),
            Foreground = AccentBrush,
            Background = CreateSolidBrush(238, 229, 214)
        };
        var roundedStyle = TryCreateRoundedSliderStyle();
        if (roundedStyle is not null)
        {
            slider.Style = roundedStyle;
        }

        return slider;
    }

    private static TextBlock CreatePillText(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 10,
            FontWeight = FontWeights.Black,
            Foreground = SoftInkBrush,
            Background = WarmCardBrush,
            Padding = new Thickness(10, 4, 10, 4),
            HorizontalAlignment = WpfHorizontalAlignment.Left
        };
    }

    private static Border CreateWarmNote(string text)
    {
        return CreateCard(new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Foreground = SoftInkBrush,
            FontWeight = FontWeights.SemiBold
        }, new Thickness(0, 0, 0, 12), CreateSolidBrush(255, 248, 231));
    }

    private static Border CreateCard(UIElement child, Thickness margin, WpfBrush background)
    {
        return new Border
        {
            Background = background,
            BorderBrush = BorderLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = margin,
            Child = child
        };
    }

    private static Style CreateRoundedButtonStyle()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(15));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(BorderThicknessProperty));

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(HorizontalAlignmentProperty, WpfHorizontalAlignment.Center);
        content.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        content.SetValue(MarginProperty, new TemplateBindingExtension(PaddingProperty));
        border.AppendChild(content);

        return new Style(typeof(WpfButton))
        {
            Setters =
            {
                new Setter(TemplateProperty, new ControlTemplate(typeof(WpfButton)) { VisualTree = border }),
                new Setter(CursorProperty, System.Windows.Input.Cursors.Hand)
            }
        };
    }

    private static Style CreateWarmScrollBarStyle()
    {
        const string scrollBarStyle = """
            <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                   xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                   TargetType="{x:Type ScrollBar}">
                <Setter Property="Width" Value="10"/>
                <Setter Property="MinWidth" Value="10"/>
                <Setter Property="Background" Value="Transparent"/>
                <Setter Property="Opacity" Value="0"/>
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type ScrollBar}">
                            <Grid Width="10" Background="Transparent">
                                <Track x:Name="PART_Track" IsDirectionReversed="True">
                                    <Track.DecreaseRepeatButton>
                                        <RepeatButton Command="{x:Static ScrollBar.LineUpCommand}"
                                                      IsHitTestVisible="False"
                                                      Opacity="0"/>
                                    </Track.DecreaseRepeatButton>
                                    <Track.IncreaseRepeatButton>
                                        <RepeatButton Command="{x:Static ScrollBar.LineDownCommand}"
                                                      IsHitTestVisible="False"
                                                      Opacity="0"/>
                                    </Track.IncreaseRepeatButton>
                                    <Track.Thumb>
                                        <Thumb Width="6">
                                            <Thumb.Template>
                                                <ControlTemplate TargetType="{x:Type Thumb}">
                                                    <Border Width="6"
                                                            Margin="2,4"
                                                            CornerRadius="6"
                                                            Background="#D7C2A0"/>
                                                </ControlTemplate>
                                            </Thumb.Template>
                                        </Thumb>
                                    </Track.Thumb>
                                </Track>
                            </Grid>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
                <Style.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter Property="Opacity" Value="1"/>
                    </Trigger>
                </Style.Triggers>
            </Style>
            """;

        return (Style)XamlReader.Parse(scrollBarStyle);
    }

    private void ShowScrollChrome()
    {
        SetScrollBarsOpacity(this, 1);
        _scrollChromeTimer.Stop();
        _scrollChromeTimer.Start();
    }

    private static void SetScrollBarsOpacity(DependencyObject root, double opacity)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is WpfScrollBar scrollBar)
            {
                scrollBar.Opacity = scrollBar.IsMouseOver ? 1 : opacity;
            }

            SetScrollBarsOpacity(child, opacity);
        }
    }

    private static Style CreateRoundedComboBoxStyle()
    {
        var outer = new FrameworkElementFactory(typeof(Border));
        outer.SetValue(Border.CornerRadiusProperty, new CornerRadius(16));
        outer.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
        outer.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));
        outer.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(BorderThicknessProperty));

        var grid = new FrameworkElementFactory(typeof(Grid));
        grid.SetValue(MinHeightProperty, 40.0);
        outer.AppendChild(grid);

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetBinding(
            ContentPresenter.ContentProperty,
            new WpfBinding("SelectedItem.Content") { RelativeSource = RelativeSource.TemplatedParent });
        content.SetValue(HorizontalAlignmentProperty, WpfHorizontalAlignment.Left);
        content.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        content.SetValue(MarginProperty, new Thickness(14, 0, 42, 0));
        grid.AppendChild(content);

        var arrow = new FrameworkElementFactory(typeof(TextBlock));
        arrow.SetValue(TextBlock.TextProperty, "⌄");
        arrow.SetValue(TextBlock.FontSizeProperty, 18.0);
        arrow.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        arrow.SetValue(TextBlock.ForegroundProperty, SoftInkBrush);
        arrow.SetValue(HorizontalAlignmentProperty, WpfHorizontalAlignment.Right);
        arrow.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        arrow.SetValue(MarginProperty, new Thickness(0, 0, 14, 2));
        grid.AppendChild(arrow);

        var popup = new FrameworkElementFactory(typeof(Popup));
        popup.SetValue(Popup.NameProperty, "PART_Popup");
        popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
        popup.SetValue(Popup.AllowsTransparencyProperty, true);
        popup.SetValue(Popup.FocusableProperty, false);
        popup.SetValue(Popup.IsOpenProperty, new TemplateBindingExtension(WpfComboBox.IsDropDownOpenProperty));

        var dropBorder = new FrameworkElementFactory(typeof(Border));
        dropBorder.SetValue(Border.BackgroundProperty, CardBrush);
        dropBorder.SetValue(Border.BorderBrushProperty, BorderLineBrush);
        dropBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        dropBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(16));
        dropBorder.SetValue(Border.MarginProperty, new Thickness(0, 6, 0, 0));
        dropBorder.SetValue(Border.PaddingProperty, new Thickness(6));
        dropBorder.SetValue(FrameworkElement.MinWidthProperty, new TemplateBindingExtension(ActualWidthProperty));
        dropBorder.SetValue(EffectProperty, new DropShadowEffect
        {
            BlurRadius = 18,
            Direction = 270,
            Opacity = 0.14,
            ShadowDepth = 8,
            Color = MediaColor.FromRgb(130, 111, 92)
        });

        var scroll = new FrameworkElementFactory(typeof(ScrollViewer));
        scroll.SetValue(ScrollViewer.CanContentScrollProperty, true);
        var presenter = new FrameworkElementFactory(typeof(ItemsPresenter));
        scroll.AppendChild(presenter);
        dropBorder.AppendChild(scroll);
        popup.AppendChild(dropBorder);
        grid.AppendChild(popup);

        return new Style(typeof(WpfComboBox))
        {
            Setters =
            {
                new Setter(TemplateProperty, new ControlTemplate(typeof(WpfComboBox)) { VisualTree = outer }),
                new Setter(ItemsControl.ItemContainerStyleProperty, CreateRoundedComboBoxItemStyle())
            }
        };
    }

    private static Style CreateRoundedComboBoxItemStyle()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
        border.SetValue(Border.PaddingProperty, new Thickness(10, 8, 10, 8));
        border.SetValue(Border.MarginProperty, new Thickness(0, 1, 0, 1));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.HorizontalAlignmentProperty, WpfHorizontalAlignment.Left);
        content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(content);

        var template = new ControlTemplate(typeof(ComboBoxItem)) { VisualTree = border };
        var selected = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
        selected.Setters.Add(new Setter(BackgroundProperty, WarmCardBrush));
        var hover = new Trigger { Property = ComboBoxItem.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(BackgroundProperty, CreateSolidBrush(255, 248, 231)));
        template.Triggers.Add(selected);
        template.Triggers.Add(hover);

        return new Style(typeof(ComboBoxItem))
        {
            Setters =
            {
                new Setter(TemplateProperty, template),
                new Setter(ForegroundProperty, InkBrush),
                new Setter(FontWeightProperty, FontWeights.SemiBold),
                new Setter(BackgroundProperty, WpfBrushes.Transparent)
            }
        };
    }

    private static Style? TryCreateRoundedSliderStyle()
    {
        try
        {
            return CreateRoundedSliderStyle();
        }
        catch (XamlParseException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static Style CreateRoundedSliderStyle()
    {
        const string sliderStyle = """
            <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                   xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                   TargetType="{x:Type Slider}">
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type Slider}">
                            <Grid MinHeight="30">
                                <Track x:Name="PART_Track" VerticalAlignment="Center">
                                    <Track.DecreaseRepeatButton>
                                        <RepeatButton Command="{x:Static Slider.DecreaseLarge}" Height="8">
                                            <RepeatButton.Template>
                                                <ControlTemplate TargetType="{x:Type RepeatButton}">
                                                    <Border Height="8" CornerRadius="6" Background="#F8CE70"/>
                                                </ControlTemplate>
                                            </RepeatButton.Template>
                                        </RepeatButton>
                                    </Track.DecreaseRepeatButton>
                                    <Track.IncreaseRepeatButton>
                                        <RepeatButton Command="{x:Static Slider.IncreaseLarge}" Height="8">
                                            <RepeatButton.Template>
                                                <ControlTemplate TargetType="{x:Type RepeatButton}">
                                                    <Border Height="8" CornerRadius="6" Background="#EEE5D6"/>
                                                </ControlTemplate>
                                            </RepeatButton.Template>
                                        </RepeatButton>
                                    </Track.IncreaseRepeatButton>
                                    <Track.Thumb>
                                        <Thumb Width="22" Height="22">
                                            <Thumb.Template>
                                                <ControlTemplate TargetType="{x:Type Thumb}">
                                                    <Border Width="22"
                                                            Height="22"
                                                            CornerRadius="11"
                                                            Background="#FFFDF8"
                                                            BorderBrush="#1F1F22"
                                                            BorderThickness="3">
                                                        <Border.Effect>
                                                            <DropShadowEffect BlurRadius="10"
                                                                              Direction="270"
                                                                              Opacity="0.18"
                                                                              ShadowDepth="3"
                                                                              Color="#826F5C"/>
                                                        </Border.Effect>
                                                    </Border>
                                                </ControlTemplate>
                                            </Thumb.Template>
                                        </Thumb>
                                    </Track.Thumb>
                                </Track>
                            </Grid>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>
            """;

        return (Style)XamlReader.Parse(sliderStyle);
    }

    private static SolidColorBrush CreateSolidBrush(byte red, byte green, byte blue)
    {
        return new SolidColorBrush(MediaColor.FromRgb(red, green, blue));
    }

    private static string FormatWeights(CatHabitWeights weights)
    {
        var normalized = weights.Normalized();
        return $"休息 {normalized.RestWeight:P0} / 活动 {normalized.ActivityWeight:P0} / 陪伴 {normalized.AccompanyWeight:P0}";
    }

    private static string DominantLabel(CatHabitWeights weights)
    {
        var normalized = weights.Normalized();
        if (normalized.RestWeight >= normalized.ActivityWeight && normalized.RestWeight >= normalized.AccompanyWeight)
        {
            return "休息";
        }

        return normalized.ActivityWeight >= normalized.AccompanyWeight
            ? "活动"
            : "陪伴";
    }

    private static string BucketLabel(CatTimeBucket bucket)
    {
        return bucket switch
        {
            CatTimeBucket.Morning => "早晨 05:00-12:00",
            CatTimeBucket.Afternoon => "下午 12:00-18:00",
            CatTimeBucket.Evening => "晚上 18:00-23:00",
            CatTimeBucket.Night => "夜间 23:00-05:00",
            _ => bucket.ToString()
        };
    }

    private static string BucketShortLabel(CatTimeBucket bucket)
    {
        return bucket switch
        {
            CatTimeBucket.Morning => "早晨",
            CatTimeBucket.Afternoon => "下午",
            CatTimeBucket.Evening => "晚上",
            CatTimeBucket.Night => "夜间",
            _ => bucket.ToString()
        };
    }
}
