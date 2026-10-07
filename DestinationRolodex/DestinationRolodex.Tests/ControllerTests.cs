using DestinationRolodex.Input;
using DestinationRolodex.Models;
using DestinationRolodex.Rendering;
using SkiaSharp;

namespace DestinationRolodex.Tests;

public class CountryPickerControllerTests
{
    private static CountryPickerController Picker(int count = 6)
    {
        var p = new CountryPickerController();
        p.Reset(count, 0);
        return p;
    }

    private static void Settle(CountryPickerController p, double seconds = 3)
    {
        var t = 0d;
        while (t < seconds)
        {
            p.Tick(1 / 60d, t);
            t += 1 / 60d;
        }
    }

    [Test]
    public void Previous_from_first_wraps_to_last_and_next_returns()
    {
        var p = Picker();
        var selections = new List<int>();
        p.SelectionChanged += (i, _) => selections.Add(i);
        p.Step(-1, true);
        Settle(p);
        p.SelectedIndex.Should().Be(5);
        p.Step(1, true);
        Settle(p);
        p.SelectedIndex.Should().Be(0);
        selections.Should().Equal(5, 0);
    }

    [Test]
    public void Thousand_steps_keep_position_bounded_and_land_correctly()
    {
        var p = Picker();
        for (var i = 0; i < 1000; i++)
        {
            p.Step(1, true);
        }

        Settle(p, 5);
        p.SelectedIndex.Should().Be(1000 % 6);
        Math.Abs(p.Position).Should().BeLessThan(6 * 1000 + 10);
        p.IsSettled.Should().BeTrue();
    }

    [Test]
    public void GoTo_takes_the_shortest_path_and_ties_go_forward()
    {
        var p = Picker();
        p.GoTo(5);
        p.Target.Should().Be(-1);
        Settle(p);
        p.SelectedIndex.Should().Be(5);

        var q = Picker();
        q.GoTo(3);
        q.Target.Should().Be(3);
    }

    [Test]
    public void Spring_settles_within_two_seconds_without_overshooting_a_row()
    {
        var p = Picker();
        p.Step(1, true);
        var t = 0d;
        var maxPosition = 0d;
        while (t < 2)
        {
            p.Tick(1 / 60d, t);
            maxPosition = Math.Max(maxPosition, p.Position);
            t += 1 / 60d;
        }

        p.IsSettled.Should().BeTrue();
        maxPosition.Should().BeLessThan(1.5);
        p.Position.Should().BeApproximately(1, 1e-3);
    }

    [Test]
    public void Wheel_snaps_to_a_row_after_the_idle_delay()
    {
        var p = Picker();
        p.Wheel(60, 0);
        p.Target.Should().BeApproximately(60 / 115d, 1e-9);
        p.Tick(1 / 60d, .05);
        p.Target.Should().BeApproximately(60 / 115d, 1e-9);
        p.Tick(1 / 60d, .2);
        p.Target.Should().Be(1);
    }

    [Test]
    public void Wheel_delta_is_clamped_per_event()
    {
        var p = Picker();
        p.Wheel(10000, 0);
        p.Target.Should().BeApproximately(180 / 115d, 1e-9);
    }

    [Test]
    public void Short_unmoved_press_on_a_row_selects_it()
    {
        var p = Picker();
        p.DragStart(10, 10, 0, row: 2);
        p.DragMove(11, 11, .02);
        p.DragEnd(.05, cancelled: false);
        p.Target.Should().Be(2);
    }

    [Test]
    public void Drag_follows_displacement_and_release_momentum_is_bounded()
    {
        var p = Picker();
        p.RowPitch = 80;
        p.DragStart(0, 0, 0, null);
        p.DragMove(0, -80, .016);
        p.Position.Should().BeApproximately(1, 1e-9);
        p.DragMove(0, -400, .032);
        p.DragEnd(.04, cancelled: false);
        Math.Abs(p.Velocity).Should().BeLessThanOrEqualTo(3.0000001);
        (p.Target - 5).Should().BeLessThanOrEqualTo(1.6);
    }

    [Test]
    public void Cancelled_drag_never_leaves_the_picker_stuck()
    {
        var p = Picker();
        p.DragStart(0, 0, 0, null);
        p.DragMove(0, -40, .016);
        p.DragEnd(.02, cancelled: true);
        p.IsDragging.Should().BeFalse();
        p.Velocity.Should().Be(0);
        Settle(p);
        p.IsSettled.Should().BeTrue();
    }

    [Test]
    public void Reduced_motion_updates_immediately()
    {
        var p = Picker();
        p.ReducedMotion = true;
        p.Step(2, true);
        p.Position.Should().Be(2);
        p.SelectedIndex.Should().Be(2);
    }

    [Test]
    public void Type_ahead_accumulates_within_timeout()
    {
        var p = Picker();
        var countries = new[] { "Philippines", "Japan", "Italy", "Norway", "Iceland", "New Zealand" };
        p.TypeAhead('n', countries, 0).Should().Be(3);
        p.TypeAhead('e', countries, .3).Should().Be(5);
        p.TypeAhead('i', countries, 2).Should().Be(2);
    }

    [Test]
    public void Single_item_and_empty_collections_are_deliberate()
    {
        var one = Picker(1);
        one.Step(1, true);
        Settle(one);
        one.SelectedIndex.Should().Be(0);

        var none = new CountryPickerController();
        none.Reset(0, 0);
        none.Step(1, true);
        none.SelectedIndex.Should().Be(-1);
        none.Tick(1 / 60d, 0).Should().BeFalse();
    }
}

public class TransitionCoordinatorTests
{
    private static readonly Destination A = Dest("a");
    private static readonly Destination B = Dest("b");
    private static readonly Destination C = Dest("c");

    private static Destination Dest(string id) => new(id, id, id, "r", "d", new Uri($"ms-appx:///Assets/{id}.jpg"), FocalPoint.Center, id, string.Empty, null, "#000000");

    private static DecodedPhoto Photo(string id)
    {
        using var bmp = new SKBitmap(4, 3);
        return new DecodedPhoto(id, SKImage.FromBitmap(bmp));
    }

    [Test]
    public void Latest_target_wins_without_an_unbounded_queue()
    {
        var c = new TransitionCoordinator();
        using var a = Photo("a");
        using var b = Photo("b");
        using var cc = Photo("c");
        var settings = new TransitionSettings(PhotoTransitionKind.MorphBlend, TimeSpan.FromMilliseconds(1000), .6);
        var started = new List<string>();
        c.Started += t => started.Add($"{t.FromDestination.Id}->{t.ToDestination.Id}");

        c.ShowImmediately(A, a);
        c.Present(B, b, 1, settings);
        c.Present(C, cc, 1, settings);
        c.Present(A, a, -1, settings);
        c.Present(C, cc, 1, settings);

        c.Active!.ToDestination.Id.Should().Be("b");
        c.Tick(1001).Should().BeTrue();
        c.Active!.ToDestination.Id.Should().Be("c");
        c.HasQueuedTarget.Should().BeFalse();
        c.Tick(1001).Should().BeFalse();
        c.DisplayedDestination!.Id.Should().Be("c");
        started.Should().Equal("a->b", "b->c");
    }

    [Test]
    public void Endpoints_and_progress_follow_elapsed_time()
    {
        var c = new TransitionCoordinator();
        using var a = Photo("a");
        using var b = Photo("b");
        var settings = new TransitionSettings(PhotoTransitionKind.LiquidWarp, TimeSpan.FromMilliseconds(1200), .6);
        c.ShowImmediately(A, a);
        c.Present(B, b, 1, settings);
        c.FrameInputs().Progress.Should().Be(0);
        c.Tick(600);
        c.FrameInputs().Progress.Should().BeApproximately(.5f, 1e-4f);
        c.FrameInputs().Kind.Should().Be(PhotoTransitionKind.LiquidWarp);
        c.Tick(600);
        c.Active.Should().BeNull();
        c.FrameInputs().From.Should().BeSameAs(b);
    }

    [Test]
    public void Seek_holds_a_pair_and_cancel_preview_keeps_the_displayed_photo()
    {
        var c = new TransitionCoordinator();
        using var a = Photo("a");
        using var b = Photo("b");
        var settings = TransitionSettings.Default;
        c.ShowImmediately(A, a);
        c.Seek(A, a, B, b, 1, settings, .25);
        c.FrameInputs().Progress.Should().BeApproximately(.25f, 1e-4f);
        c.Tick(500);
        c.FrameInputs().Progress.Should().BeApproximately(.25f, 1e-4f);
        c.CancelPreview();
        c.DisplayedPhoto.Should().BeSameAs(a);
        c.Active.Should().BeNull();
    }

    [Test]
    public void Reduced_motion_path_shows_immediately()
    {
        var c = new TransitionCoordinator();
        using var a = Photo("a");
        using var b = Photo("b");
        c.ShowImmediately(A, a);
        c.ShowImmediately(B, b);
        c.Active.Should().BeNull();
        c.DisplayedPhoto.Should().BeSameAs(b);
    }
}

public class CameraControllerTests
{
    [Test]
    public void Zoom_is_clamped_and_reset_restores_identity()
    {
        var cam = new CameraController();
        cam.SetZoom(5);
        cam.TargetZoom.Should().Be(1.65);
        cam.SetZoom(0);
        cam.TargetZoom.Should().Be(1);
        cam.SetPointer(3, -3);
        cam.TargetX.Should().Be(1);
        cam.TargetY.Should().Be(-1);
        for (var i = 0; i < 300; i++) cam.Tick(1 / 60d);
        cam.Pose.TranslateX.Should().BeApproximately(12, .05f);
        cam.Reset();
        for (var i = 0; i < 300; i++) cam.Tick(1 / 60d);
        cam.Pose.TranslateX.Should().BeApproximately(0, .05f);
    }

    [Test]
    public void Reduced_motion_removes_tilt_and_browse_parallax()
    {
        var cam = new CameraController { ReducedMotion = true };
        cam.SetPointer(1, 1);
        for (var i = 0; i < 300; i++) cam.Tick(1 / 60d);
        cam.Pose.Should().Be(CameraPose.Identity with { Zoom = 1 });
    }
}

public class RendererMathTests
{
    [Test]
    public void Camera_matrix_identity_maps_points_unchanged()
    {
        var m = PhotoRenderer.CameraMatrix(CameraPose.Identity, 240, 265);
        var p = m.MapPoint(new SKPoint(10, 20));
        p.X.Should().BeApproximately(10, 1e-3f);
        p.Y.Should().BeApproximately(20, 1e-3f);
    }

    [Test]
    public void Camera_matrix_zoom_scales_about_the_center()
    {
        var m = PhotoRenderer.CameraMatrix(new CameraPose(0, 0, 0, 0, 2), 100, 100);
        m.MapPoint(new SKPoint(100, 100)).Should().Be(new SKPoint(100, 100));
        var p = m.MapPoint(new SKPoint(150, 100));
        p.X.Should().BeApproximately(200, 1e-3f);
    }

    [Test]
    public void Cover_crop_matches_shader_formula()
    {
        var (ox, oy, sx, sy) = PhotoRenderer.CoverCrop(1.5f, 480f / 530f, new FocalPoint(.5, .52));
        sx.Should().BeApproximately(480f / 530f / 1.5f, 1e-5f);
        sy.Should().Be(1);
        ox.Should().BeApproximately((1 - sx) * .5f, 1e-5f);
        oy.Should().Be(0);
    }
}
