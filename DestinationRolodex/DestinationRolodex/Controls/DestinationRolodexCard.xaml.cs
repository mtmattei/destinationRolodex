using System.Diagnostics;
using DestinationRolodex.Input;
using DestinationRolodex.Models;
using DestinationRolodex.Rendering;
using DestinationRolodex.Services;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Uno.WinUI.Graphics2DSK;
using Windows.Foundation;
using Windows.System;

namespace DestinationRolodex.Controls;

// xaml-lint: allow codebehind - reusable custom control: pointer capture, wheel normalization, keyboard navigation, the
// frame loop and Skia invalidation are the control's implementation and have no XAML surface. Hosts bind its
// dependency properties and events; they never touch this file.
/// <summary>
/// Destination Rolodex card: looping country picker over a procedural photo transition (Morph Blend / Liquid Warp),
/// pointer parallax, Look around, tour autoplay and a white detail panel. Two instances share no state.
/// </summary>
public sealed partial class DestinationRolodexCard : UserControl
{
    private const double MinAutoPlayIntervalMs = 2500;
    private const double LoadingGraceMs = 250;

    private readonly CountryPickerController _picker = new();
    private readonly CameraController _camera = new();
    private readonly TransitionCoordinator _coordinator = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(8) };
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Tween _detailOpacity = new(1);
    private readonly Tween _detailY = new(0);
    private readonly Tween _pickerOpacity = new(1);
    private readonly Tween _pickerY = new(0);
    private readonly Tween _lookOpacity = new(0);
    private readonly Tween _lookY = new(6);
    private readonly Tween _readability = new(1);
    private readonly List<Button> _dots = new();
    private readonly List<double> _frameIntervals = new();

    private IReadOnlyList<Destination> _data = Array.Empty<Destination>();
    private DestinationMediaCache? _cache;
    private PhotoTransitionSurface? _surface;
    private Image? _fallbackImage;
    private int _index = -1;
    private int _mediaToken;
    private int _previewTicket;
    private bool _loaded;
    private bool _visible = true;
    private bool _hostVisible = true;
    private bool _auto;
    private bool _autoExplicit;
    private bool _hover;
    private bool _focused;
    private bool _previewing;
    private bool _suppressTransition;
    private bool _syncingSelectedIndex;
    private double _autoProgress;
    private double? _lastTick;
    private Pointer? _capturedPointer;
    private Point _dragOrigin;
    private double _dragPanX, _dragPanY;
    private bool _dragIsLook;
    private (Destination From, Destination To)? _lastPair;
    private (Destination From, Destination To)? _previewPair;
    private DateTime? _statusDue;
    private DateTime? _graceDue;
    private Destination? _pendingDestination;
    private Size _lastViewport;
    private FontFamily? _fontSemiBold, _fontMedium, _fontRegular;
    private Brush? _sceneInk, _regionInk;

    public DestinationRolodexCard()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => Tick();
        _picker.PositionChanged += () => Picker.Render(_picker.Position);
        _picker.SelectionChanged += OnPickerSelectionChanged;
        _coordinator.Started += t => TransitionStarted?.Invoke(this, new TransitionEventArgs(t.FromDestination, t.ToDestination, t.Settings.Kind, t.Settings.Duration, t.IsPreview, t.Direction));
        _coordinator.Completed += OnTransitionCompleted;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += (_, _) => UpdateLayoutMetrics();
        EffectiveViewportChanged += OnEffectiveViewportChanged;

        Scene.PointerWheelChanged += OnSceneWheel;
        Scene.PointerPressed += OnScenePointerPressed;
        Scene.PointerMoved += OnScenePointerMoved;
        Scene.PointerReleased += OnScenePointerReleased;
        Scene.PointerCanceled += (s, e) => EndDrag(e, cancelled: true);
        Scene.PointerCaptureLost += (s, e) => EndDrag(e, cancelled: true);
        Scene.PointerEntered += OnScenePointerEntered;
        Scene.PointerExited += OnScenePointerExited;
        Root.KeyDown += OnKeyDown;
        Root.GotFocus += (_, _) => { _focused = true; Wake(); };
        Root.LostFocus += (_, _) => { _focused = XamlRoot is { } xr && FocusManager.GetFocusedElement(xr) is DependencyObject d && IsDescendant(d); Wake(); };
        CreditLink.Click += (_, _) => { if (_index >= 0 && _data[_index].PhotoSource is { } uri) { _ = Windows.System.Launcher.LaunchUriAsync(uri); } };
    }

    #region Dependency properties

    public static readonly DependencyProperty DestinationsProperty = DependencyProperty.Register(
        nameof(Destinations), typeof(IReadOnlyList<Destination>), typeof(DestinationRolodexCard), new PropertyMetadata(null, (d, _) => ((DestinationRolodexCard)d).OnDestinationsChanged()));

    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
        nameof(SelectedIndex), typeof(int), typeof(DestinationRolodexCard), new PropertyMetadata(0, (d, e) => ((DestinationRolodexCard)d).OnSelectedIndexChanged((int)e.NewValue)));

    public static readonly DependencyProperty TransitionKindProperty = DependencyProperty.Register(
        nameof(TransitionKind), typeof(PhotoTransitionKind), typeof(DestinationRolodexCard), new PropertyMetadata(PhotoTransitionKind.MorphBlend, (d, _) => ((DestinationRolodexCard)d).UpdateMotionBadge()));

    public static readonly DependencyProperty TransitionDurationProperty = DependencyProperty.Register(
        nameof(TransitionDuration), typeof(TimeSpan), typeof(DestinationRolodexCard), new PropertyMetadata(TimeSpan.FromMilliseconds(1200)));

    public static readonly DependencyProperty TransitionStrengthProperty = DependencyProperty.Register(
        nameof(TransitionStrength), typeof(double), typeof(DestinationRolodexCard), new PropertyMetadata(.6));

    public static readonly DependencyProperty IsAutoPlayEnabledProperty = DependencyProperty.Register(
        nameof(IsAutoPlayEnabled), typeof(bool), typeof(DestinationRolodexCard), new PropertyMetadata(false, (d, e) => ((DestinationRolodexCard)d).OnAutoPlayEnabledChanged((bool)e.NewValue)));

    public static readonly DependencyProperty AutoPlayIntervalProperty = DependencyProperty.Register(
        nameof(AutoPlayInterval), typeof(TimeSpan), typeof(DestinationRolodexCard), new PropertyMetadata(TimeSpan.FromMilliseconds(5500)));

    public static readonly DependencyProperty IsReducedMotionEnabledProperty = DependencyProperty.Register(
        nameof(IsReducedMotionEnabled), typeof(bool), typeof(DestinationRolodexCard), new PropertyMetadata(false, (d, _) => ((DestinationRolodexCard)d).OnReducedMotionChanged()));

    public static readonly DependencyProperty AllowRuntimeEffectProperty = DependencyProperty.Register(
        nameof(AllowRuntimeEffect), typeof(bool), typeof(DestinationRolodexCard), new PropertyMetadata(true));

    public static readonly DependencyProperty ViewModeProperty = DependencyProperty.Register(
        nameof(ViewMode), typeof(CardViewMode), typeof(DestinationRolodexCard), new PropertyMetadata(CardViewMode.Browse, (d, e) => ((DestinationRolodexCard)d).ApplyViewMode((CardViewMode)e.NewValue)));

    public static readonly DependencyProperty RendererStatusProperty = DependencyProperty.Register(
        nameof(RendererStatus), typeof(PhotoRendererStatus), typeof(DestinationRolodexCard), new PropertyMetadata(PhotoRendererStatus.Unavailable));

    public static readonly DependencyProperty MediaStatusProperty = DependencyProperty.Register(
        nameof(MediaStatus), typeof(DestinationMediaStatus), typeof(DestinationRolodexCard), new PropertyMetadata(DestinationMediaStatus.Empty));

    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(
        nameof(IsPlaying), typeof(bool), typeof(DestinationRolodexCard), new PropertyMetadata(false));

    public static readonly DependencyProperty SceneHeightProperty = DependencyProperty.Register(
        nameof(SceneHeight), typeof(double), typeof(DestinationRolodexCard), new PropertyMetadata(double.NaN, (d, _) => ((DestinationRolodexCard)d).UpdateLayoutMetrics()));

    /// <summary>The ordered catalog. Replacing it invalidates loads and keeps the selection by id when possible.</summary>
    public IReadOnlyList<Destination>? Destinations { get => (IReadOnlyList<Destination>?)GetValue(DestinationsProperty); set => SetValue(DestinationsProperty, value); }
    public int SelectedIndex { get => (int)GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }
    public PhotoTransitionKind TransitionKind { get => (PhotoTransitionKind)GetValue(TransitionKindProperty); set => SetValue(TransitionKindProperty, value); }
    public TimeSpan TransitionDuration { get => (TimeSpan)GetValue(TransitionDurationProperty); set => SetValue(TransitionDurationProperty, value); }
    public double TransitionStrength { get => (double)GetValue(TransitionStrengthProperty); set => SetValue(TransitionStrengthProperty, value); }
    /// <summary>Host setting. Starts the tour passively (hover/focus pause it). Off when motion is reduced.</summary>
    public bool IsAutoPlayEnabled { get => (bool)GetValue(IsAutoPlayEnabledProperty); set => SetValue(IsAutoPlayEnabledProperty, value); }
    public TimeSpan AutoPlayInterval { get => (TimeSpan)GetValue(AutoPlayIntervalProperty); set => SetValue(AutoPlayIntervalProperty, value); }
    /// <summary>OS policy plus application setting: no procedural transition, parallax, drift or default autoplay.</summary>
    public bool IsReducedMotionEnabled { get => (bool)GetValue(IsReducedMotionEnabledProperty); set => SetValue(IsReducedMotionEnabledProperty, value); }
    /// <summary>Diagnostics: false forces the plain-image capability path even where the runtime effect compiles.</summary>
    public bool AllowRuntimeEffect { get => (bool)GetValue(AllowRuntimeEffectProperty); set => SetValue(AllowRuntimeEffectProperty, value); }
    public CardViewMode ViewMode { get => (CardViewMode)GetValue(ViewModeProperty); set => SetValue(ViewModeProperty, value); }
    public PhotoRendererStatus RendererStatus { get => (PhotoRendererStatus)GetValue(RendererStatusProperty); private set => SetValue(RendererStatusProperty, value); }
    public DestinationMediaStatus MediaStatus { get => (DestinationMediaStatus)GetValue(MediaStatusProperty); private set => SetValue(MediaStatusProperty, value); }
    public bool IsPlaying { get => (bool)GetValue(IsPlayingProperty); private set => SetValue(IsPlayingProperty, value); }
    /// <summary>Explicit scene height; NaN lets the card clamp from the viewport height like the prototype.</summary>
    public double SceneHeight { get => (double)GetValue(SceneHeightProperty); set => SetValue(SceneHeightProperty, value); }

    public Destination? SelectedDestination => _index >= 0 && _index < _data.Count ? _data[_index] : null;
    public TransitionSettings Settings => new(TransitionKind, TransitionDuration, TransitionStrength);
    public string? RendererDetail { get; private set; }

    #endregion

    #region Events

    public event EventHandler<DestinationChangedEventArgs>? SelectionChanged;
    public event EventHandler<TransitionEventArgs>? TransitionStarted;
    public event EventHandler<TransitionProgressEventArgs>? TransitionProgress;
    public event EventHandler<TransitionEventArgs>? TransitionCompleted;
    public event EventHandler<string>? RendererFallback;
    public event EventHandler<string>? MediaFailed;
    public event EventHandler<CardViewMode>? ViewModeChanged;

    public sealed record DestinationChangedEventArgs(int Index, Destination Destination, bool UserInitiated);
    public sealed record TransitionEventArgs(Destination From, Destination To, PhotoTransitionKind Kind, TimeSpan Duration, bool IsPreview, int Direction);
    public sealed record TransitionProgressEventArgs(double Progress, bool IsPreview, Destination From, Destination To);

    #endregion

    #region Lifecycle

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _loaded = true;
        _fontSemiBold ??= (FontFamily)Application.Current.Resources["RolodexFontSemiBold"];
        _fontMedium ??= (FontFamily)Application.Current.Resources["RolodexFontMedium"];
        _fontRegular ??= (FontFamily)Application.Current.Resources["RolodexFontRegular"];
        _sceneInk ??= (Brush)Application.Current.Resources["RolodexSceneTextBrushFixed"];
        _regionInk ??= (Brush)Application.Current.Resources["RolodexRegionBrushFixed"];
        GlyphLayout.Apply(Eyebrow, "DESTINATIONS", _fontSemiBold, 10, 2.2, _sceneInk);
        UpdateMotionBadge();
#if DEBUG
        if (App.HarnessOptions.NoShader)
        {
            AllowRuntimeEffect = false;
        }
#endif
        EnsureSurface();
        _cache ??= new DestinationMediaCache();
        ApplyHarnessHooks();
        if (XamlRoot is { } root)
        {
            root.Changed += OnXamlRootChanged;
            _hostVisible = root.IsHostVisible;
        }

        UpdateLayoutMetrics();
        InitializeData();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _loaded = false;
        _timer.Stop();
        _lastTick = null;
        _mediaToken++;
        _previewTicket++;
        _previewing = false;
        if (XamlRoot is { } root)
        {
            root.Changed -= OnXamlRootChanged;
        }

        _coordinator.Clear();
        if (_surface is not null)
        {
            PhotoHost.Child = null;
            _surface.Release();
            _surface = null;
        }

        _fallbackImage = null;
        PhotoHost.Child = null;
        _cache?.Dispose();
        _cache = null;
    }

    private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        _hostVisible = sender.IsHostVisible;
        UpdateLayoutMetrics();
        Wake();
    }

    private void OnEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
    {
        var viewport = args.EffectiveViewport;
        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
        bounds.Intersect(viewport);
        _visible = !bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0;
        Wake();
    }

    private void EnsureSurface()
    {
        if (_surface is not null || _fallbackImage is not null)
        {
            return;
        }

        if (SKCanvasElement.IsSupportedOnCurrentPlatform())
        {
            _surface = new PhotoTransitionSurface(AllowRuntimeEffect);
            PhotoHost.Child = _surface;
            if (_surface.Renderer.HasRuntimeEffect)
            {
                RendererStatus = PhotoRendererStatus.Shader;
                RendererDetail = "Runtime SkSL effect";
            }
            else
            {
                RendererStatus = PhotoRendererStatus.PlainImage;
                RendererDetail = _surface.Renderer.CompileError ?? "Runtime effect unavailable";
                RendererFallback?.Invoke(this, $"Runtime effect unavailable. Showing plain photos with a crossfade. {RendererDetail}");
            }
        }
        else
        {
            _fallbackImage = new Image { Stretch = Stretch.UniformToFill, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            PhotoHost.Child = _fallbackImage;
            PhotoHost.Margin = new Thickness(-PhotoRenderer.OverscanPerSide * 480, -PhotoRenderer.OverscanPerSide * 480, -PhotoRenderer.OverscanPerSide * 480, -PhotoRenderer.OverscanPerSide * 480);
            RendererStatus = PhotoRendererStatus.Unavailable;
            RendererDetail = "SKCanvasElement is not supported on this target";
            RendererFallback?.Invoke(this, "Skia surface unavailable on this target. Showing plain photos.");
        }

        UpdateMotionBadge();
    }

    private void ApplyHarnessHooks()
    {
#if DEBUG
        var h = App.HarnessOptions;
        if (_cache is not null)
        {
            if (h.MediaDelayMs > 0) _cache.ArtificialDelay = TimeSpan.FromMilliseconds(h.MediaDelayMs);
            if (h.MediaFail is { } fail) foreach (var id in fail.Split(',', StringSplitOptions.RemoveEmptyEntries)) _cache.FailingIds.Add(id);
        }
#endif
    }

    #endregion

    #region Data

    private void OnDestinationsChanged()
    {
        if (_loaded)
        {
            InitializeData();
        }
    }

    private void InitializeData()
    {
        var previousId = SelectedDestination?.Id;
        var incoming = Destinations ?? Array.Empty<Destination>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in incoming)
        {
            if (!ids.Add(d.Id))
            {
                throw new ArgumentException($"Duplicate destination id: {d.Id}", nameof(Destinations));
            }
        }

        _mediaToken++;
        _previewTicket++;
        _previewing = false;
        _lastPair = null;
        _previewPair = null;
        _coordinator.Clear();
        _cache?.Clear();
        _cache?.Pinned.Clear();
        _data = incoming;
        _index = -1;

        var start = previousId is not null ? Math.Max(0, IndexOf(previousId)) : Math.Clamp(SelectedIndex, 0, Math.Max(0, _data.Count - 1));
        var hasData = _data.Count > 0;
        EmptyState.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
        Shadow.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
        CounterTotal.Text = _data.Count.ToString("00");
        var canNavigate = _data.Count > 1;
        PrevButton.IsEnabled = NextButton.IsEnabled = PlayButton.IsEnabled = canNavigate;
        RebuildIndicators();
        _picker.ReducedMotion = IsReducedMotionEnabled;
        _camera.ReducedMotion = IsReducedMotionEnabled;
        Picker.Configure(_data.Select(d => d.Country).ToList(), _picker.RowPitch, 60, 54, 300, false);
        UpdateLayoutMetrics();
        _autoExplicit = false;
        _auto = IsAutoPlayEnabled && !IsReducedMotionEnabled && canNavigate;
        _autoProgress = 0;
        SyncPlayButton();
        _picker.Reset(_data.Count, start);
        if (!hasData)
        {
            MediaStatus = DestinationMediaStatus.Empty;
            PresentFrame();
        }

        Wake();
    }

    private int IndexOf(string id)
    {
        for (var i = 0; i < _data.Count; i++)
        {
            if (_data[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    private void RebuildIndicators()
    {
        Pagination.Children.Clear();
        _dots.Clear();
        var inactive = (Brush)Application.Current.Resources["RolodexPaginationInactiveBrushFixed"];
        for (var i = 0; i < _data.Count; i++)
        {
            var index = i;
            var bar = new Border { Width = 16, Height = 3, CornerRadius = new CornerRadius(4), Background = inactive, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var button = new Button { Style = (Style)Application.Current.Resources["RolodexBareButton"], Width = 28, Height = 44, MinWidth = 28, Content = bar };
            AutomationProperties.SetName(button, $"Show {_data[i].Country}");
            ToolTipService.SetToolTip(button, _data[i].Country);
            button.Click += (_, _) => GoTo(index);
            _dots.Add(button);
            Pagination.Children.Add(button);
        }
    }

    private void UpdateIndicators()
    {
        var active = (Brush)Application.Current.Resources["RolodexPaginationActiveBrushFixed"];
        var inactive = (Brush)Application.Current.Resources["RolodexPaginationInactiveBrushFixed"];
        for (var i = 0; i < _dots.Count; i++)
        {
            var selected = i == _index;
            if (_dots[i].Content is Border bar)
            {
                bar.Width = selected ? 24 : 16;
                bar.Background = selected ? active : inactive;
            }

            AutomationProperties.SetItemStatus(_dots[i], selected ? "selected" : string.Empty);
        }
    }

    #endregion

    #region Layout metrics (container-relative sizing; brief 02)

    // xaml-lint: allow responsive - these thresholds are container queries on the card's own width (two cards can sit
    // side by side at different widths) and viewport-height clamps; {utu:Responsive} only switches on window width.
    private void UpdateLayoutMetrics()
    {
        if (!_loaded)
        {
            return;
        }

        var cardWidth = Root.ActualWidth > 0 ? Root.ActualWidth : 480;
        var viewport = XamlRoot?.Size ?? new Size(1440, 1040);
        _lastViewport = viewport;
        var narrowHost = viewport.Width <= 560;
        double sceneHeight;
        if (!double.IsNaN(SceneHeight) && SceneHeight > 0)
        {
            sceneHeight = Math.Max(340, SceneHeight);
        }
        else if (narrowHost)
        {
            sceneHeight = Math.Clamp(viewport.Height * .55, 340, 510);
        }
        else if (viewport.Height <= 760)
        {
            sceneHeight = 360;
        }
        else
        {
            sceneHeight = Math.Clamp(viewport.Height * .58, 365, 530);
        }

        Scene.Height = sceneHeight;
        SceneClip.Rect = new Rect(0, 0, cardWidth, sceneHeight);
        var radius = narrowHost ? 23 : 26;
        CardBorder.CornerRadius = new CornerRadius(radius);
        Shadow.CornerRadius = new CornerRadius(radius);
        Scene.CornerRadius = new CornerRadius(radius, radius, 0, 0);
        Readability.Margin = new Thickness(0, sceneHeight * .17, 0, sceneHeight * .12);

        var compact = cardWidth <= 360;
        var windowHeight = compact ? 266 : 300;
        _picker.RowPitch = Math.Clamp(cardWidth * .17, 58, 80);
        var fontSize = Math.Clamp(cardWidth * .1245, 36, 64);
        var longFontSize = Math.Clamp(cardWidth * .113, 34, 59);
        Picker.Configure(_data.Select(d => d.Country).ToList(), _picker.RowPitch, fontSize, longFontSize, windowHeight, compact);
        Picker.Margin = new Thickness(0, sceneHeight * .495 - windowHeight / 2d, 0, 0);
        Picker.Render(_picker.Position, force: true);

        TopBar.Margin = compact ? new Thickness(20, 20, 16, 0) : new Thickness(26, 20, 20, 0);
        SceneFooter.Margin = compact ? new Thickness(19, 0, 16, 21) : new Thickness(24, 0, 21, 21);
        Detail.Padding = compact ? (Thickness)Application.Current.Resources["RolodexDetailPaddingSmall"] : (Thickness)Application.Current.Resources["RolodexDetailPadding"];
        DetailBottom.Margin = new Thickness(0, compact ? 9 : 13, 0, 0);
        Counter.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        DescriptionText.FontSize = compact ? 12 : 13;
        LocationText.FontSize = compact ? 11 : 12;
        GlyphLayout.Apply(Eyebrow, "DESTINATIONS", _fontSemiBold!, compact ? 9 : 10, compact ? 1.5 : 2.2, _sceneInk!);
        if (_fallbackImage is not null)
        {
            var ox = PhotoRenderer.OverscanPerSide * cardWidth;
            var oy = PhotoRenderer.OverscanPerSide * sceneHeight;
            PhotoHost.Margin = new Thickness(-ox, -oy, -ox, -oy);
        }

        UpdateHint();
        PresentFrame();
    }

    private void UpdateHint()
    {
        var coarse = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
        var text = coarse ? "SWIPE TO EXPLORE" : "SCROLL TO EXPLORE";
        var compact = Root.ActualWidth > 0 && Root.ActualWidth <= 360;
        GlyphLayout.Apply(HintText, text, _fontMedium!, compact ? 8 : 9, compact ? .8 : 1.3, _sceneInk!);
    }

    private void UpdateMotionBadge()
    {
        if (!_loaded || _fontRegular is null || _sceneInk is null)
        {
            return;
        }

        var text = RendererStatus switch
        {
            PhotoRendererStatus.Shader => TransitionKind == PhotoTransitionKind.LiquidWarp ? "LIQUID WARP" : "MORPH BLEND",
            PhotoRendererStatus.PlainImage => "PLAIN PHOTO · NO SHADER",
            _ => "PLAIN PHOTO · NO SKIA SURFACE",
        };
        GlyphLayout.Apply(MotionBadge, text, _fontRegular, 8, 1.25, _sceneInk);
    }

    #endregion

    #region Selection

    private void OnPickerSelectionChanged(int index, bool user)
    {
        _index = index;
        var d = _data[index];
        CounterCurrent.Text = (index + 1).ToString("00");
        LocationText.Text = d.Location;
        GlyphLayout.Apply(RegionText, d.Region.ToUpperInvariant(), _fontMedium!, Root.ActualWidth <= 360 ? 8 : 9, Root.ActualWidth <= 360 ? 1 : 1.45, _regionInk!);
        DescriptionText.Text = d.Description;
        LookCountry.Text = d.Country;
        CreditText.Text = string.IsNullOrEmpty(d.PhotoCredit) ? "View photo source" : $"Photo: {d.PhotoCredit}";
        CreditLink.IsEnabled = d.PhotoSource is not null;
        Scene.Background = new SolidColorBrush(ParseColor(d.FallbackColor));
        UpdateIndicators();
        if (!IsReducedMotionEnabled)
        {
            _detailOpacity.Restart(.22, 1, .32);
            _detailY.Restart(4, 0, .32);
        }
        else
        {
            _detailOpacity.Snap(1);
            _detailY.Snap(0);
        }

        if (user)
        {
            Announcer.Text = $"{d.Country}. {d.Description}";
        }

        if (!_syncingSelectedIndex)
        {
            _syncingSelectedIndex = true;
            SelectedIndex = index;
            _syncingSelectedIndex = false;
        }

        ShowMedia(d);
        Preload(index);
        SelectionChanged?.Invoke(this, new DestinationChangedEventArgs(index, d, user));
        Wake();
    }

    private void OnSelectedIndexChanged(int value)
    {
        if (_syncingSelectedIndex || !_loaded || _data.Count == 0)
        {
            return;
        }

        if (value < 0 || value >= _data.Count)
        {
            return;
        }

        if (value != _index)
        {
            GoTo(value);
        }
    }

    public void Next() => Step(1, true);
    public void Previous() => Step(-1, true);

    private void Step(int step, bool user)
    {
        if (_data.Count == 0)
        {
            return;
        }

        if (user)
        {
            ExitPreview();
            Pause();
        }

        _autoProgress = 0;
        _picker.Step(step, user);
        Wake();
    }

    /// <summary>Shortest path to the destination at <paramref name="index"/>; ties resolve forward.</summary>
    public void GoTo(int index)
    {
        if (_data.Count == 0)
        {
            return;
        }

        ExitPreview();
        Pause();
        _autoProgress = 0;
        _picker.GoTo(index);
        Wake();
    }

    #endregion

    #region Media

    private void Preload(int index)
    {
        if (_cache is null || _data.Count < 2)
        {
            return;
        }

        foreach (var offset in new[] { -1, 1 })
        {
            var d = _data[CircularMath.PositiveModulo(index + offset, _data.Count)];
            if (_cache.Peek(d.Id) is null)
            {
                _ = _cache.GetAsync(d).ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        PinWorkingSet();
    }

    private void PinWorkingSet()
    {
        if (_cache is null)
        {
            return;
        }

        _cache.Pinned.Clear();
        if (_index >= 0 && _data.Count > 0)
        {
            _cache.Pinned.Add(_data[_index].Id);
            _cache.Pinned.Add(_data[CircularMath.PositiveModulo(_index - 1, _data.Count)].Id);
            _cache.Pinned.Add(_data[CircularMath.PositiveModulo(_index + 1, _data.Count)].Id);
        }

        if (_coordinator.DisplayedDestination is { } shown) _cache.Pinned.Add(shown.Id);
        if (_coordinator.Active is { } t) { _cache.Pinned.Add(t.FromDestination.Id); _cache.Pinned.Add(t.ToDestination.Id); }
    }

    private async void ShowMedia(Destination d, bool force = false)
    {
        if (_previewing || _cache is null)
        {
            return;
        }

        var token = ++_mediaToken;
        SetStatus(null, retry: false);
        _statusDue = null;
        _graceDue = null;
        _pendingDestination = null;

        if (!force && _cache.Peek(d.Id) is { } ready)
        {
            Mount(d, ready, token);
            return;
        }

        MediaStatus = DestinationMediaStatus.Loading;
        _pendingDestination = d;
        _graceDue = DateTime.UtcNow.AddMilliseconds(LoadingGraceMs);
        _statusDue = DateTime.UtcNow.AddMilliseconds(1100);
        Wake();
        try
        {
            var photo = await _cache.GetAsync(d);
            if (token != _mediaToken || !_loaded)
            {
                return;
            }

            Mount(d, photo, token);
        }
        catch (Exception ex)
        {
            if (token != _mediaToken || !_loaded)
            {
                return;
            }

            _graceDue = null;
            _statusDue = null;
            _pendingDestination = null;
            MediaStatus = DestinationMediaStatus.Failed;
            _coordinator.Clear();
            SetStatus("Photo unavailable · check connection", retry: true);
            MediaFailed?.Invoke(this, ex.Message);
            PresentFrame();
        }
    }

    private void Mount(Destination d, DecodedPhoto photo, int token)
    {
        if (token != _mediaToken)
        {
            return;
        }

        _graceDue = null;
        _statusDue = null;
        _pendingDestination = null;
        SetStatus(null, retry: false);
        MediaStatus = DestinationMediaStatus.Ready;

        var canTransition = _coordinator.DisplayedPhoto is not null
                            && !ReferenceEquals(_coordinator.DisplayedPhoto, photo)
                            && !IsReducedMotionEnabled
                            && !_suppressTransition
                            && !_previewing
                            && RendererStatus != PhotoRendererStatus.Unavailable;
        if (canTransition)
        {
            var from = _coordinator.Active?.ToDestination ?? _coordinator.DisplayedDestination!;
            _lastPair = (from, d);
            var settings = RendererStatus == PhotoRendererStatus.Shader
                ? Settings
                : new TransitionSettings(TransitionKind, TimeSpan.FromMilliseconds(400), 0);
            _coordinator.Present(d, photo, _picker.LastDirection, settings);
        }
        else
        {
            _coordinator.ShowImmediately(d, photo);
        }

        if (_fallbackImage is not null)
        {
            _fallbackImage.Source = new BitmapImage(d.ImageUri);
        }

        PinWorkingSet();
        PresentFrame();
        Wake();
    }

    private void SetStatus(string? message, bool retry)
    {
        MediaStatusPill.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
        StatusText.Text = message ?? string.Empty;
        RetryButton.Visibility = retry ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Retries the selected destination's photo after a failure.</summary>
    public void Retry()
    {
        if (_index < 0 || _cache is null)
        {
            return;
        }

        var d = _data[_index];
        _cache.Invalidate(d.Id);
        ShowMedia(d, force: true);
    }

    #endregion

    #region Autoplay

    public void Play()
    {
        ExitPreview();
        if (_data.Count < 2)
        {
            return;
        }

        if (ViewMode == CardViewMode.Look)
        {
            ViewMode = CardViewMode.Browse;
        }

        _auto = true;
        _autoExplicit = true;
        _autoProgress = 0;
        SyncPlayButton();
        Wake();
    }

    public void Pause()
    {
        _auto = false;
        _autoExplicit = false;
        _autoProgress = 0;
        SyncPlayButton();
    }

    private void OnAutoPlayEnabledChanged(bool enabled)
    {
        if (!_loaded)
        {
            return;
        }

        if (enabled && !IsReducedMotionEnabled && _data.Count > 1)
        {
            _auto = true;
            _autoExplicit = false;
            _autoProgress = 0;
            SyncPlayButton();
            Wake();
        }
        else if (!enabled)
        {
            Pause();
        }
    }

    private void SyncPlayButton()
    {
        IsPlaying = _auto;
        var label = _auto ? "Pause destination tour" : "Play destination tour";
        AutomationProperties.SetName(PlayButton, label);
        ToolTipService.SetToolTip(PlayButton, label);
        PlayGlyph.Visibility = _auto ? Visibility.Collapsed : Visibility.Visible;
        PauseGlyph.Visibility = _auto ? Visibility.Visible : Visibility.Collapsed;
        PlayRing.StrokeDashOffset = 113.1;
    }

    #endregion

    #region View mode and camera

    private void ApplyViewMode(CardViewMode mode)
    {
        if (!_loaded)
        {
            return;
        }

        ExitPreview();
        Pause();
        var look = mode == CardViewMode.Look;
        _camera.IsLooking = look;
        Picker.IsInert = look;
        Scene.IsTabStop = look;
        AutomationProperties.SetName(Scene, look ? "Explore destination photo. Arrow keys pan; plus and minus zoom; Escape returns to countries." : string.Empty);
        AutomationProperties.SetName(LookButton, look ? "Back to list" : "Explore this photo");
        LookLabel.Text = look ? "Back to list" : "Look around";
        OrbitGlyph.Visibility = look ? Visibility.Collapsed : Visibility.Visible;
        BackGlyph.Visibility = look ? Visibility.Visible : Visibility.Collapsed;
        CameraControls.Visibility = look ? Visibility.Visible : Visibility.Collapsed;
        GestureHint.Visibility = look ? Visibility.Collapsed : Visibility.Visible;
        CreditLink.Visibility = look ? Visibility.Visible : Visibility.Collapsed;
        PlayButton.Visibility = look ? Visibility.Collapsed : Visibility.Visible;
        MotionBadge.Visibility = look ? Visibility.Collapsed : Visibility.Visible;
        var reduced = IsReducedMotionEnabled;
        _pickerOpacity.Go(look ? 0 : 1, reduced ? 0 : .36);
        _pickerY.Go(look ? -Picker.ActualHeight * .04 : 0, reduced ? 0 : .6);
        _lookOpacity.Go(look ? 1 : 0, reduced ? 0 : .35);
        _lookY.Go(look ? 0 : 6, reduced ? 0 : .4);
        _readability.Go(look ? 0 : 1, reduced ? 0 : .4);
        ResetCamera();
        if (look)
        {
            Scene.Focus(FocusState.Programmatic);
        }

        ViewModeChanged?.Invoke(this, mode);
        Wake();
    }

    public void ZoomIn() => SetZoom(_camera.TargetZoom + .15);
    public void ZoomOut() => SetZoom(_camera.TargetZoom - .15);

    private void SetZoom(double zoom)
    {
        _camera.SetZoom(zoom);
        ZoomValue.Text = $"{_camera.TargetZoom:0.0}×";
        Wake();
    }

    public void ResetCamera()
    {
        _camera.Reset();
        ZoomValue.Text = "1.0×";
        Wake();
    }

    #endregion

    #region Reduced motion

    private void OnReducedMotionChanged()
    {
        var reduced = IsReducedMotionEnabled;
        _picker.ReducedMotion = reduced;
        _camera.ReducedMotion = reduced;
        if (!_loaded)
        {
            return;
        }

        if (reduced)
        {
            ExitPreview();
            _coordinator.CancelAll();
            Pause();
            ResetCamera();
            _picker.SnapToNearest();
            _detailOpacity.Snap(1);
            _detailY.Snap(0);
        }

        if (_index >= 0)
        {
            ShowMedia(_data[_index], force: false);
        }

        Picker.Render(_picker.Position, force: true);
        Wake();
    }

    #endregion

    #region Input

    private double Now => _clock.Elapsed.TotalSeconds;

    private void OnSceneWheel(object sender, PointerRoutedEventArgs e)
    {
        if (_data.Count == 0 || e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
        {
            return;
        }

        var point = e.GetCurrentPoint(Scene);
        if (point.Properties.IsHorizontalMouseWheel)
        {
            return;
        }

        e.Handled = true;
        ExitPreview();
        // Uno reports ±120 per notch; the prototype's math expects the browser's 100 per notch, positive = down.
        var cssDelta = -point.Properties.MouseWheelDelta * (100d / 120d);
        if (ViewMode == CardViewMode.Look)
        {
            SetZoom(_camera.TargetZoom - cssDelta * .0012);
            return;
        }

        Pause();
        _picker.Wheel(cssDelta, Now);
        Wake();
    }

    private void OnScenePointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_data.Count == 0 || _capturedPointer is not null)
        {
            return;
        }

        var point = e.GetCurrentPoint(Scene);
        if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse && !point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        ExitPreview();
        Pause();
        _capturedPointer = e.Pointer;
        _dragOrigin = point.Position;
        _dragIsLook = ViewMode == CardViewMode.Look;
        if (_dragIsLook)
        {
            _dragPanX = _camera.TargetX;
            _dragPanY = _camera.TargetY;
        }
        else
        {
            var pickerPoint = Scene.TransformToVisual(Picker).TransformPoint(point.Position);
            var row = Picker.HitTestRow(pickerPoint);
            _picker.DragStart(point.Position.X, point.Position.Y, Now, row);
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                Picker.Focus(FocusState.Pointer);
            }
        }

        Scene.CapturePointer(e.Pointer);
        e.Handled = true;
        Wake();
    }

    private void OnScenePointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Scene);
        if (_capturedPointer is not null && e.Pointer.PointerId == _capturedPointer.PointerId)
        {
            var dx = point.Position.X - _dragOrigin.X;
            var dy = point.Position.Y - _dragOrigin.Y;
            if (_dragIsLook)
            {
                _camera.PanBy(_dragPanX, _dragPanY, dx, dy);
            }
            else
            {
                _picker.DragMove(point.Position.X, point.Position.Y, Now);
            }

            e.Handled = true;
        }
        else if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse && ViewMode == CardViewMode.Browse && !IsReducedMotionEnabled)
        {
            var w = Scene.ActualWidth;
            var h = Scene.ActualHeight;
            if (w > 0 && h > 0)
            {
                _camera.SetPointer((point.Position.X / w - .5) * 2, (point.Position.Y / h - .5) * 2);
            }
        }

        Wake();
    }

    private void OnScenePointerReleased(object sender, PointerRoutedEventArgs e) => EndDrag(e, cancelled: false);

    private void EndDrag(PointerRoutedEventArgs e, bool cancelled)
    {
        if (_capturedPointer is null || e.Pointer.PointerId != _capturedPointer.PointerId)
        {
            return;
        }

        _capturedPointer = null;
        Scene.ReleasePointerCapture(e.Pointer);
        if (!_dragIsLook)
        {
            _picker.DragEnd(Now, cancelled);
        }

        Wake();
    }

    private void OnScenePointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
        {
            _hover = true;
            Wake();
        }
    }

    private void OnScenePointerExited(object sender, PointerRoutedEventArgs e)
    {
        _hover = false;
        if (_capturedPointer is null && ViewMode == CardViewMode.Browse)
        {
            _camera.ClearPointer();
        }

        Wake();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape && ViewMode == CardViewMode.Look)
        {
            e.Handled = true;
            ViewMode = CardViewMode.Browse;
            Picker.Focus(FocusState.Keyboard);
            return;
        }

        if (!ReferenceEquals(e.OriginalSource, Picker) && !ReferenceEquals(e.OriginalSource, Scene))
        {
            return;
        }

        if (ViewMode == CardViewMode.Look)
        {
            switch (e.Key)
            {
                case VirtualKey.Left: _camera.Nudge(-.18, 0); break;
                case VirtualKey.Right: _camera.Nudge(.18, 0); break;
                case VirtualKey.Up: _camera.Nudge(0, -.18); break;
                case VirtualKey.Down: _camera.Nudge(0, .18); break;
                case VirtualKey.Add: case (VirtualKey)187: SetZoom(_camera.TargetZoom + .15); break;
                case VirtualKey.Subtract: case (VirtualKey)189: SetZoom(_camera.TargetZoom - .15); break;
                default: return;
            }

            e.Handled = true;
            Wake();
            return;
        }

        if (_data.Count == 0)
        {
            return;
        }

        switch (e.Key)
        {
            case VirtualKey.Down: Step(1, true); break;
            case VirtualKey.Up: Step(-1, true); break;
            case VirtualKey.PageDown: Step(3, true); break;
            case VirtualKey.PageUp: Step(-3, true); break;
            case VirtualKey.Home: GoTo(0); break;
            case VirtualKey.End: GoTo(_data.Count - 1); break;
            default:
                if (e.Key >= VirtualKey.A && e.Key <= VirtualKey.Z)
                {
                    var match = _picker.TypeAhead((char)('a' + (e.Key - VirtualKey.A)), _data.Select(d => d.Country).ToList(), Now);
                    if (match >= 0)
                    {
                        GoTo(match);
                        break;
                    }
                }

                return;
        }

        e.Handled = true;
    }

    private void OnPlayClick(object sender, RoutedEventArgs e) { if (_auto) Pause(); else Play(); }
    private void OnLookClick(object sender, RoutedEventArgs e) => ViewMode = ViewMode == CardViewMode.Browse ? CardViewMode.Look : CardViewMode.Browse;
    private void OnZoomInClick(object sender, RoutedEventArgs e) => ZoomIn();
    private void OnZoomOutClick(object sender, RoutedEventArgs e) => ZoomOut();
    private void OnResetClick(object sender, RoutedEventArgs e) => ResetCamera();
    private void OnRetryClick(object sender, RoutedEventArgs e) => Retry();
    private void OnPreviousClick(object sender, RoutedEventArgs e) => Previous();
    private void OnNextClick(object sender, RoutedEventArgs e) => Next();

    private bool IsDescendant(DependencyObject element)
    {
        var e = element;
        while (e is not null)
        {
            if (ReferenceEquals(e, this)) return true;
            e = VisualTreeHelper.GetParent(e);
        }

        return false;
    }

    #endregion

    #region Debug replay / scrub (demo panel only)

    private async Task<(Destination From, Destination To, DecodedPhoto FromPhoto, DecodedPhoto ToPhoto)?> PreparePreviewAsync()
    {
        if (_data.Count < 2 || _cache is null)
        {
            return null;
        }

        Pause();
        if (ViewMode == CardViewMode.Look)
        {
            ViewMode = CardViewMode.Browse;
        }

        var pair = _lastPair ?? (_data[Math.Max(0, _index)], _data[CircularMath.PositiveModulo(Math.Max(0, _index) + 1, _data.Count)]);
        if (RendererStatus != PhotoRendererStatus.Shader || IsReducedMotionEnabled)
        {
            GoTo(IndexOf(pair.To.Id));
            RendererFallback?.Invoke(this, IsReducedMotionEnabled ? "Reduced motion is enabled." : "Runtime effect unavailable. Using the plain-image path.");
            return null;
        }

        var ticket = ++_previewTicket;
        _mediaToken++;
        _statusDue = null;
        _graceDue = null;
        SetStatus(null, retry: false);
        _previewing = true;
        _picker.IsFrozen = true;
        _previewPair = pair;
        _lastPair = pair;
        try
        {
            var from = await _cache.GetAsync(pair.From);
            var to = await _cache.GetAsync(pair.To);
            if (ticket != _previewTicket)
            {
                return null;
            }

            return (pair.From, pair.To, from, to);
        }
        catch
        {
            if (ticket == _previewTicket)
            {
                _previewing = false;
                _picker.IsFrozen = false;
                RendererFallback?.Invoke(this, "Photos need to finish loading before replay.");
            }

            return null;
        }
    }

    /// <summary>Debug: replays the last transition pair (or current → next) with the current settings.</summary>
    public async Task<bool> ReplayTransitionAsync()
    {
        _coordinator.CancelPreview();
        var pair = await PreparePreviewAsync();
        if (pair is null)
        {
            return false;
        }

        _camera.Reset();
        var direction = PreviewDirection(pair.Value.From, pair.Value.To);
        _coordinator.Replay(pair.Value.From, pair.Value.FromPhoto, pair.Value.To, pair.Value.ToPhoto, direction, Settings);
        _picker.SyncPreview(IndexOf(pair.Value.From.Id), IndexOf(pair.Value.To.Id), 0);
        Wake();
        return true;
    }

    /// <summary>Debug: holds the last pair at a fixed progress; rows follow smoothstep(progress).</summary>
    public async Task<bool> SeekTransitionAsync(double progress)
    {
        var pair = await PreparePreviewAsync();
        if (pair is null)
        {
            return false;
        }

        var direction = PreviewDirection(pair.Value.From, pair.Value.To);
        _coordinator.Seek(pair.Value.From, pair.Value.FromPhoto, pair.Value.To, pair.Value.ToPhoto, direction, Settings, progress);
        _picker.SyncPreview(IndexOf(pair.Value.From.Id), IndexOf(pair.Value.To.Id), progress);
        PresentFrame();
        TransitionProgress?.Invoke(this, new TransitionProgressEventArgs(Math.Clamp(progress, 0, 1), true, pair.Value.From, pair.Value.To));
        Wake();
        return true;
    }

    private int PreviewDirection(Destination from, Destination to)
    {
        var delta = CircularMath.ShortestDelta(IndexOf(from.Id), IndexOf(to.Id), _data.Count);
        return delta == 0 ? 1 : Math.Sign(delta);
    }

    private void ExitPreview()
    {
        if (!_previewing)
        {
            return;
        }

        _previewTicket++;
        _coordinator.CancelPreview();
        _previewing = false;
        _picker.IsFrozen = false;
        _picker.SnapToNearest();
        var d = _data[CircularMath.PositiveModulo(CircularMath.RoundLikeJavaScript(_picker.Position), _data.Count)];
        _suppressTransition = true;
        try
        {
            ShowMedia(d, force: false);
        }
        finally
        {
            _suppressTransition = false;
        }

        Picker.Render(_picker.Position, force: true);
    }

    private void OnTransitionCompleted(TransitionCoordinator.Transition t)
    {
        if (t.IsPreview && _previewing)
        {
            _picker.SyncPreview(IndexOf(t.FromDestination.Id), IndexOf(t.ToDestination.Id), 1);
            _coordinator.ShowImmediately(t.ToDestination, t.ToPhoto);
            _previewing = false;
            _picker.IsFrozen = false;
            MediaStatus = DestinationMediaStatus.Ready;
            PinWorkingSet();
        }

        TransitionCompleted?.Invoke(this, new TransitionEventArgs(t.FromDestination, t.ToDestination, t.Settings.Kind, t.Settings.Duration, t.IsPreview, t.Direction));
    }

    #endregion

    #region Frame loop

    private void Wake()
    {
        if (!_timer.IsEnabled && _loaded && _visible && _hostVisible && _data.Count > 0)
        {
            _lastTick = null;
            _timer.Start();
        }
    }

    private void Tick()
    {
        if (!_loaded || !_visible || !_hostVisible || _data.Count == 0)
        {
            _timer.Stop();
            _lastTick = null;
            return;
        }

        if (XamlRoot is { } root && root.Size != _lastViewport)
        {
            // A window resize that reached XamlRoot.Size without a Changed/SizeChanged callback (seen during harness resizes).
            UpdateLayoutMetrics();
        }

        var now = Now;
        var dt = _lastTick is { } last ? Math.Min(now - last, .032) : 1d / 60;
        if (_lastTick is { } prev)
        {
            _frameIntervals.Add((now - prev) * 1000);
            if (_frameIntervals.Count > 2000) _frameIntervals.RemoveAt(0);
        }

        _lastTick = now;

        var moving = !_previewing && _picker.Tick(dt, now);

        var canAuto = _auto && ViewMode == CardViewMode.Browse && _capturedPointer is null && (_autoExplicit || (!_hover && !_focused));
        if (canAuto)
        {
            var interval = Math.Max(MinAutoPlayIntervalMs, AutoPlayInterval.TotalMilliseconds);
            _autoProgress += dt * 1000 / interval;
            if (_autoProgress >= 1)
            {
                _autoProgress = 0;
                Step(1, user: false);
            }
        }

        PlayRing.StrokeDashOffset = 113.1 * (1 - _autoProgress);

        _camera.IsDrifting = _auto && !IsReducedMotionEnabled && ViewMode == CardViewMode.Browse;
        var cameraMoving = _camera.Tick(dt);

        var transitioning = _coordinator.Tick(dt * 1000);
        if (_coordinator.Active is { } active)
        {
            if (active.IsPreview && _previewing)
            {
                _picker.SyncPreview(IndexOf(active.FromDestination.Id), IndexOf(active.ToDestination.Id), active.Progress);
            }

            TransitionProgress?.Invoke(this, new TransitionProgressEventArgs(active.Progress, active.IsPreview, active.FromDestination, active.ToDestination));
        }

        // Loading policy: keep the previous photo for a short grace, then show the destination's own colour with an explicit status.
        if (_pendingDestination is { } pending)
        {
            if (_graceDue is { } grace && DateTime.UtcNow >= grace)
            {
                _graceDue = null;
                _coordinator.Clear();
            }

            if (_statusDue is { } due && DateTime.UtcNow >= due)
            {
                _statusDue = null;
                SetStatus($"Loading {pending.Country} photo…", retry: false);
            }
        }

        var tweens = _detailOpacity.Tick(dt) | _detailY.Tick(dt) | _pickerOpacity.Tick(dt) | _pickerY.Tick(dt) | _lookOpacity.Tick(dt) | _lookY.Tick(dt) | _readability.Tick(dt);
        DetailCopy.Opacity = _detailOpacity.Value;
        DetailTranslate.Y = _detailY.Value;
        Picker.Opacity = _pickerOpacity.Value;
        PickerTranslate.Y = _pickerY.Value;
        LookTitle.Opacity = _lookOpacity.Value;
        LookTitleTranslate.Y = _lookY.Value;
        Readability.Opacity = _readability.Value;

        PresentFrame();
        if (_cache is not null && _surface is not null)
        {
            _cache.ObserveFrames(_surface.Renderer.FramesRendered);
            _cache.Collect(_surface.Renderer.FramesRendered);
        }

        var pendingWork = _pendingDestination is not null && (_graceDue is not null || _statusDue is not null);
        if (!(moving || cameraMoving || canAuto || transitioning || tweens || _camera.IsDrifting || pendingWork))
        {
            _timer.Stop();
            _lastTick = null;
        }
    }

    private void PresentFrame()
    {
        if (_surface is null)
        {
            return;
        }

        var backdrop = _index >= 0 && _index < _data.Count ? ParseSkColor(_data[_index].FallbackColor) : SKColor.Parse("#295c65");
        var (from, to, focalA, focalB, progress, kind, strength, direction) = _coordinator.FrameInputs();
        _surface.Present(new PhotoFrame(from, to, focalA, focalB, progress, strength, direction, kind, _camera.Pose, backdrop));
    }

    #endregion

    #region Diagnostics

    /// <summary>Frame-loop statistics for the evidence record: loop intervals and RenderOverride durations.</summary>
    public (int Frames, double P50Ms, double P95Ms, double MaxMs, double RenderP50Ms, double RenderP95Ms, double RenderMaxMs) GetFrameStatistics()
    {
        var intervals = _frameIntervals.OrderBy(v => v).ToArray();
        var renders = (_surface?.Renderer.RecentRenderMilliseconds ?? Array.Empty<double>()).OrderBy(v => v).ToArray();
        double P(double[] s, double p) => s.Length == 0 ? 0 : s[Math.Min(s.Length - 1, (int)Math.Floor(p * (s.Length - 1)))];
        return (intervals.Length, P(intervals, .5), P(intervals, .95), intervals.Length > 0 ? intervals[^1] : 0, P(renders, .5), P(renders, .95), renders.Length > 0 ? renders[^1] : 0);
    }

    public (int Count, long Bytes, int Retired) GetCacheStatistics() => _cache is null ? (0, 0, 0) : (_cache.Count, _cache.Bytes, _cache.RetiredCount);

    #endregion

    private static Windows.UI.Color ParseColor(string hex)
    {
        var c = ParseSkColor(hex);
        return Windows.UI.Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue);
    }

    private static SKColor ParseSkColor(string hex) => SKColor.TryParse(hex, out var c) ? c : SKColor.Parse("#244e51");
}
