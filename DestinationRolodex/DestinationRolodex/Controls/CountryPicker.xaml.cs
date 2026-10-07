using DestinationRolodex.Models;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace DestinationRolodex.Controls;

// xaml-lint: allow codebehind - custom control implementation: row recycling and transforms are driven per frame from the card's loop.
/// <summary>
/// The infinite country list: a fixed pool of nine rows posed from a continuous position (brief 03).
/// The card owns input and selection; this control only renders rows, markers and the focus ring.
/// </summary>
public sealed partial class CountryPicker : UserControl
{
    public const int RowPool = 9;

    private readonly StackPanel[] _rows;
    private readonly CompositeTransform[] _transforms;
    private IReadOnlyList<string> _countries = Array.Empty<string>();
    private double _rowPitch = 76;
    private double _fontSize = 60;
    private double _longFontSize = 54;
    private double _windowHeight = 300;
    private double _lastPosition;
    private FontFamily? _font;
    private Brush? _ink;

    public CountryPicker()
    {
        InitializeComponent();
        _rows = new[] { Row0, Row1, Row2, Row3, Row4, Row5, Row6, Row7, Row8 };
        _transforms = new CompositeTransform[RowPool];
        for (var i = 0; i < RowPool; i++)
        {
            _transforms[i] = new CompositeTransform();
            _rows[i].RenderTransform = _transforms[i];
        }

        SizeChanged += (_, e) => WindowClip.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
        GotFocus += (_, _) => UpdateFocusRing();
        LostFocus += (_, _) => UpdateFocusRing();
    }

    /// <summary>True while the picker should accept browse keys (false in Look around).</summary>
    public bool IsInert
    {
        get => !IsTabStop;
        set
        {
            IsTabStop = !value;
            IsHitTestVisible = !value;
        }
    }

    public void Configure(IReadOnlyList<string> countries, double rowPitch, double fontSize, double longFontSize, double windowHeight, bool compact)
    {
        _countries = countries;
        _rowPitch = rowPitch;
        _fontSize = fontSize;
        _longFontSize = longFontSize;
        _windowHeight = windowHeight;
        Height = windowHeight;
        FocusRing.Margin = new Thickness(13, windowHeight / 2 - 44, 13, windowHeight / 2 - 44);
        MarkerLeft.Margin = new Thickness(compact ? 10 : 15, 0, 0, 0);
        MarkerDot.Width = MarkerDot.Height = compact ? 6 : 7;
        MarkerRight.Width = compact ? 9 : 12;
        MarkerRight.Margin = new Thickness(0, 0, compact ? 14 : 19, 0);
        _font ??= (FontFamily)Application.Current.Resources["RolodexFontSemiBold"];
        _ink ??= (Brush)Application.Current.Resources["RolodexSceneTextBrushFixed"];
        Render(_lastPosition, force: true);
    }

    /// <summary>Poses the nine rows around <paramref name="position"/> (prototype _renderRows, plus the mask folded into opacity).</summary>
    public void Render(double position, bool force = false)
    {
        _lastPosition = position;
        var n = _countries.Count;
        if (n == 0 || _font is null || _ink is null)
        {
            foreach (var row in _rows)
            {
                row.Visibility = Visibility.Collapsed;
            }

            return;
        }

        var baseIndex = (long)Math.Floor(position);
        for (var i = 0; i < RowPool; i++)
        {
            var integer = baseIndex + i - 4;
            var d = integer - position;
            var pose = CircularMath.Pose(d, _rowPitch);
            var country = _countries[CircularMath.PositiveModulo(integer, n)];
            var row = _rows[i];
            var isLong = country.Length > 11;
            var size = isLong ? _longFontSize : _fontSize;
            GlyphLayout.Apply(row, country, _font, size, -.057 * size, _ink, size * 1.12);
            row.Tag = integer; // row step for click-to-select (GlyphLayout keys by text, so Tag is safe to reuse after Apply)

            // Perspective fold: translateZ(-13a) at 900 px and rotateX(-12d) collapse into the scale (≤ 1.6 % trapezoid, see SPEC.md).
            var a = Math.Abs(d);
            var zFactor = 900d / (900d + 13d * a);
            var t = _transforms[i];
            t.TranslateY = pose.Y;
            t.ScaleX = pose.Scale * zFactor;
            t.ScaleY = pose.Scale * zFactor * Math.Cos(pose.RotationX * Math.PI / 180d);

            var edge = _windowHeight / 2 - Math.Abs(pose.Y);
            var mask = Math.Clamp(edge / (.1 * _windowHeight), 0, 1);
            row.Opacity = pose.Opacity * mask;
            var hidden = !pose.Visible || (n == 1 && Math.Abs(d) > .5) || mask <= 0;
            row.Visibility = hidden ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    /// <summary>Row step under a point in this control's coordinates, or null.</summary>
    public long? HitTestRow(Point point)
    {
        foreach (var element in VisualTreeHelper.FindElementsInHostCoordinates(TransformToVisual(null).TransformPoint(point), this))
        {
            var e = element as DependencyObject;
            while (e is not null && !ReferenceEquals(e, this))
            {
                if (e is StackPanel { Tag: long step } panel && Array.IndexOf(_rows, panel) >= 0)
                {
                    return step;
                }

                e = VisualTreeHelper.GetParent(e);
            }
        }

        // Fallback: solve the row geometry analytically (rows are centered; y is relative to the window center).
        var y = point.Y - _windowHeight / 2;
        var d = y / _rowPitch;
        for (var i = 0; i < 2; i++)
        {
            d = y / (_rowPitch * (1 - Math.Min(Math.Abs(d), 3) * .035));
        }

        var nearest = CircularMath.RoundLikeJavaScript(_lastPosition + d);
        return Math.Abs(nearest - _lastPosition) < 3 ? nearest : null;
    }

    private void UpdateFocusRing() => FocusRing.Visibility = FocusState == FocusState.Keyboard ? Visibility.Visible : Visibility.Collapsed;
}
