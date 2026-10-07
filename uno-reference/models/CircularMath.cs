// Reference arithmetic port; compile and unit-test in the target solution.
using System;

namespace DestinationRolodex.Reference;

public readonly record struct RowPose(double Y, double Z, double RotationX, double Scale, double Opacity, bool Visible);

public static class CircularMath
{
    public static int PositiveModulo(long value, int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        return (int)(((value % count) + count) % count);
    }

    // JavaScript Math.round ties toward positive infinity, unlike default C# Math.Round.
    public static long RoundLikeJavaScript(double position)
    {
        if (!double.IsFinite(position) || Math.Abs(position) > 1e12)
            throw new ArgumentOutOfRangeException(nameof(position), "Normalize the circular position before it grows this large.");
        return checked((long)Math.Floor(position + .5));
    }

    public static int SelectedIndex(double position, int count) => PositiveModulo(RoundLikeJavaScript(position), count);

    // Equal forward/back distances resolve forward, matching prototype goTo().
    public static int ShortestDelta(int from, int to, int count)
    {
        if (count <= 0 || from < 0 || from >= count || to < 0 || to >= count)
            throw new ArgumentOutOfRangeException(nameof(count));
        var delta = PositiveModulo((long)to - from, count);
        return 2L * delta > count ? delta - count : delta;
    }

    public static RowPose Pose(double distance, double rowPitch)
    {
        if (!double.IsFinite(distance) || !double.IsFinite(rowPitch) || rowPitch <= 0)
            throw new ArgumentOutOfRangeException(nameof(distance));
        double a = Math.Abs(distance);
        double opacity = a <= 1 ? 1 - .5 * a : a <= 2 ? .5 - .34 * (a - 1) : .16 * (3 - a);
        return new(
            distance * rowPitch * (1 - Math.Min(a, 3) * .035),
            -a * 13,
            -distance * 12,
            Math.Max(.65, 1 - a * .044),
            Math.Clamp(opacity, 0, 1),
            a < 3);
    }
}
