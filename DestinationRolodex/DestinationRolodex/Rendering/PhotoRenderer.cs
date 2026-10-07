using System.Diagnostics;
using DestinationRolodex.Models;
using SkiaSharp;

namespace DestinationRolodex.Rendering;

/// <summary>
/// Draws the overscanned photo layer for one card. Pure SkiaSharp: no XAML, so the same code renders on the
/// SKCanvasElement window canvas and into an offscreen raster surface for fixture captures.
///
/// Ownership: the renderer owns its compiled effect, builder and paints. It never owns photos (the cache does).
/// Per frame it only sets uniforms and builds one short-lived SKShader; nothing is compiled or decoded here.
/// </summary>
public sealed class PhotoRenderer : IDisposable
{
    public const float OverscanPerSide = .13f;
    public const float Perspective = 1000f;
    public const int MaxTextureSide = 1600;

    private readonly SKRuntimeEffect? _effect;
    private readonly SKRuntimeShaderBuilder? _builder;
    private readonly SKPaint _photoPaint = new() { IsAntialias = false };
    private readonly SKPaint _backdropPaint = new() { Style = SKPaintStyle.Fill };
    private readonly SKPaint _imagePaint = new() { IsAntialias = false };
    private readonly SKSamplingOptions _sampling = new(SKFilterMode.Linear, SKMipmapMode.None);
    private readonly double[] _renderMs = new double[240];
    private int _renderCount;
    private bool _disposed;

    public PhotoRenderer(bool allowRuntimeEffect = true)
    {
        if (allowRuntimeEffect)
        {
            _effect = SKRuntimeEffect.CreateShader(TransitionShaderSource.Sksl, out var errors);
            CompileError = _effect is null ? (string.IsNullOrWhiteSpace(errors) ? "SKRuntimeEffect.CreateShader returned null." : errors) : null;
            if (_effect is not null)
            {
                _builder = new SKRuntimeShaderBuilder(_effect);
            }
        }
        else
        {
            CompileError = "Runtime effect disabled by host.";
        }
    }

    /// <summary>True when the SkSL effect compiled and the morph/liquid paths are live.</summary>
    public bool HasRuntimeEffect => _builder is not null;

    public string? CompileError { get; }

    /// <summary>Frames rendered so far; the media cache uses it as a barrier before disposing retired photos.</summary>
    public long FramesRendered => Interlocked.Read(ref _framesRendered);
    private long _framesRendered;

    /// <summary>Last render durations in milliseconds (ring buffer), for the performance evidence record.</summary>
    public IReadOnlyList<double> RecentRenderMilliseconds
    {
        get
        {
            var n = Math.Min(_renderCount, _renderMs.Length);
            var copy = new double[n];
            Array.Copy(_renderMs, copy, n);
            return copy;
        }
    }

    /// <summary>Overscanned layer rectangle for a scene of the given size (13% per side, 1.26x).</summary>
    public static SKRect LayerRect(float sceneWidth, float sceneHeight) =>
        new(-OverscanPerSide * sceneWidth, -OverscanPerSide * sceneHeight, (1 + OverscanPerSide) * sceneWidth, (1 + OverscanPerSide) * sceneHeight);

    /// <summary>
    /// CSS: perspective(1000px) translate3d(x,y,0) rotateX(rx) rotateY(ry) scale(z) with transform-origin at the scene center,
    /// projected to the z=0 plane as a 3x3 projective matrix. Same mapping for static frames and shader frames.
    /// </summary>
    public static SKMatrix CameraMatrix(CameraPose c, float centerX, float centerY)
    {
        var rx = c.RotateXDegrees * Math.PI / 180d;
        var ry = c.RotateYDegrees * Math.PI / 180d;
        double s = c.Zoom;
        double cry = Math.Cos(ry), sry = Math.Sin(ry), crx = Math.Cos(rx), srx = Math.Sin(rx);
        double d = Perspective;

        // Rows over (u, v, 1) where (u, v) is centered on the origin.
        double a00 = s * cry, a01 = 0, a02 = c.TranslateX;
        double a10 = s * sry * srx, a11 = s * crx, a12 = c.TranslateY;
        double a20 = s * sry * crx / d, a21 = -s * srx / d, a22 = 1;

        var projected = new SKMatrix(
            (float)(a00 + centerX * a20), (float)(a01 + centerX * a21), (float)(a02 + centerX * a22),
            (float)(a10 + centerY * a20), (float)(a11 + centerY * a21), (float)(a12 + centerY * a22),
            (float)a20, (float)a21, (float)a22);

        return projected.PreConcat(SKMatrix.CreateTranslation(-centerX, -centerY));
    }

    /// <summary>Cover crop in normalized top-left image space: (offsetX, offsetY, scaleX, scaleY). Mirrors crop() in the shader.</summary>
    public static (float OffsetX, float OffsetY, float ScaleX, float ScaleY) CoverCrop(float imageAspect, float layerAspect, FocalPoint focal)
    {
        float sx, sy;
        if (imageAspect > layerAspect)
        {
            sx = layerAspect / imageAspect;
            sy = 1;
        }
        else
        {
            sx = 1;
            sy = imageAspect / layerAspect;
        }

        return ((1 - sx) * (float)focal.X, (1 - sy) * (float)focal.Y, sx, sy);
    }

    public void Render(SKCanvas canvas, float sceneWidth, float sceneHeight, PhotoFrame frame)
    {
        if (_disposed || sceneWidth <= 0 || sceneHeight <= 0)
        {
            return;
        }

        var sw = Stopwatch.StartNew();
        canvas.Save();
        canvas.ClipRect(new SKRect(0, 0, sceneWidth, sceneHeight));

        _backdropPaint.Color = frame.Backdrop;
        canvas.DrawRect(0, 0, sceneWidth, sceneHeight, _backdropPaint);

        var from = frame.From is { IsDisposed: false } ? frame.From : null;
        var to = frame.To is { IsDisposed: false } ? frame.To : null;
        if (from is null && to is null)
        {
            canvas.Restore();
            RecordTiming(sw);
            return;
        }

        var a = from ?? to!;
        var b = to ?? from!;
        var progress = from is null ? 1f : to is null ? 0f : Math.Clamp(frame.Progress, 0f, 1f);

        var layer = LayerRect(sceneWidth, sceneHeight);
        var camera = CameraMatrix(frame.Camera, sceneWidth / 2f, sceneHeight / 2f);
        canvas.Concat(in camera);
        canvas.Translate(layer.Left, layer.Top);

        if (_builder is not null)
        {
            DrawWithEffect(canvas, layer.Width, layer.Height, a, b, frame, progress);
        }
        else
        {
            DrawPlain(canvas, layer.Width, layer.Height, a, b, frame, progress);
        }

        canvas.Restore();
        RecordTiming(sw);
    }

    private void DrawWithEffect(SKCanvas canvas, float w, float h, DecodedPhoto a, DecodedPhoto b, PhotoFrame frame, float progress)
    {
        var builder = _builder!;
        builder.Children["uFrom"] = a.Shader;
        builder.Children["uTo"] = b.Shader;
        builder.Uniforms["uProgress"] = progress;
        builder.Uniforms["uStrength"] = Math.Clamp(frame.Strength, 0f, 1f);
        builder.Uniforms["uDirection"] = frame.Direction < 0 ? -1f : 1f;
        builder.Uniforms["uAspect"] = w / h;
        builder.Uniforms["uAspectA"] = a.Aspect;
        builder.Uniforms["uAspectB"] = b.Aspect;
        builder.Uniforms["uFocalA"] = new[] { (float)frame.FocalA.X, (float)frame.FocalA.Y };
        builder.Uniforms["uFocalB"] = new[] { (float)frame.FocalB.X, (float)frame.FocalB.Y };
        builder.Uniforms["uMode"] = (int)frame.Mode;
        builder.Uniforms["uResolution"] = new[] { w, h };
        builder.Uniforms["uImageSizeA"] = new[] { (float)a.Width, (float)a.Height };
        builder.Uniforms["uImageSizeB"] = new[] { (float)b.Width, (float)b.Height };

        using var shader = builder.Build();
        _photoPaint.Shader = shader;
        canvas.DrawRect(0, 0, w, h, _photoPaint);
        _photoPaint.Shader = null;
    }

    /// <summary>Capability path when the runtime effect is unavailable: cover-cropped images with a plain crossfade. Never labelled as an effect.</summary>
    private void DrawPlain(SKCanvas canvas, float w, float h, DecodedPhoto a, DecodedPhoto b, PhotoFrame frame, float progress)
    {
        var dest = new SKRect(0, 0, w, h);
        var layerAspect = w / h;

        if (progress < 1)
        {
            _imagePaint.Color = SKColors.White;
            canvas.DrawImage(a.Image, SourceRect(a, layerAspect, frame.FocalA), dest, _sampling, _imagePaint);
        }

        if (progress > 0)
        {
            var eased = progress * progress * (3 - 2 * progress);
            _imagePaint.Color = SKColors.White.WithAlpha((byte)Math.Round(255 * eased));
            canvas.DrawImage(b.Image, SourceRect(b, layerAspect, frame.FocalB), dest, _sampling, _imagePaint);
        }
    }

    private static SKRect SourceRect(DecodedPhoto photo, float layerAspect, FocalPoint focal)
    {
        var (ox, oy, sx, sy) = CoverCrop(photo.Aspect, layerAspect, focal);
        return new SKRect(ox * photo.Width, oy * photo.Height, (ox + sx) * photo.Width, (oy + sy) * photo.Height);
    }

    private void RecordTiming(Stopwatch sw)
    {
        sw.Stop();
        _renderMs[_renderCount % _renderMs.Length] = sw.Elapsed.TotalMilliseconds;
        _renderCount++;
        Interlocked.Increment(ref _framesRendered);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _photoPaint.Dispose();
        _backdropPaint.Dispose();
        _imagePaint.Dispose();
        // The builder disposes its effect; it was created here, so this is the one place that may dispose it.
        _builder?.Dispose();
    }
}
