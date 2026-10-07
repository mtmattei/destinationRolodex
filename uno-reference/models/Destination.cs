// Reference contracts only. Not compiled in the packaging environment.
using System;

namespace DestinationRolodex.Reference;

public enum PhotoTransitionKind { MorphBlend = 0, LiquidWarp = 1 }
public enum CardViewMode { Browse, Look }
public enum DestinationMediaStatus { Empty, Loading, Ready, Failed }

// Coordinates are normalized, top-left-oriented; not pixel positions.
public readonly record struct FocalPoint
{
    public double X { get; }
    public double Y { get; }
    public FocalPoint(double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || x < 0 || x > 1 || y < 0 || y > 1)
            throw new ArgumentOutOfRangeException(nameof(x), "Focal coordinates must be finite values in [0, 1].");
        X = x; Y = y;
    }
}

// Map assets/destinations.json by ID through assets/manifest.json to packaged URIs.
// This record is a native repository contract, not a direct JSON deserialization claim.
public sealed record Destination(
    string Id,
    string Country,
    string Location,
    string Region,
    string Description,
    Uri ImageUri,
    FocalPoint FocalPoint,
    string ImageDescription,
    string PhotoCredit,
    Uri PhotoSource,
    string FallbackColor);

public readonly record struct TransitionSettings
{
    public PhotoTransitionKind Kind { get; }
    public TimeSpan Duration { get; }
    public double Strength { get; }
    public TransitionSettings(PhotoTransitionKind kind, TimeSpan duration, double strength)
    {
        if (kind is not PhotoTransitionKind.MorphBlend and not PhotoTransitionKind.LiquidWarp)
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (!double.IsFinite(strength)) throw new ArgumentOutOfRangeException(nameof(strength));
        Kind = kind;
        Duration = TimeSpan.FromMilliseconds(Math.Clamp(duration.TotalMilliseconds, 350, 3000));
        Strength = Math.Clamp(strength, 0, 1);
    }
    public static TransitionSettings Default => new(PhotoTransitionKind.MorphBlend, TimeSpan.FromMilliseconds(1200), .60);
}
