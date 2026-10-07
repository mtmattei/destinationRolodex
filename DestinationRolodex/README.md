# Destination Rolodex · Uno Platform

A reusable destination-browsing card for Uno Platform: a looping country picker over a photographic scene whose
photo changes through one of two procedural effects, **Morph Blend** or **Liquid Warp**, rendered by a runtime SkSL
effect on an `SKCanvasElement`. Pointer parallax, Look around (constrained pan/zoom/tilt), a tour autoplay with a
progress ring, loading/failure/retry states, reduced motion and a white detail panel. Built from the handoff kit in the
parent folder (`../briefs`, `../prototype/approved.html`).

## Stack

| | |
| --- | --- |
| .NET SDK | 10.0.303 |
| Uno.Sdk | 6.7.30 (Uno.WinUI 6.7.135, Uno.WinUI.Graphics2DSK 6.7.135, Uno.Toolkit.WinUI 9.1.3, Uno.Simple 7.1.1) |
| SkiaSharp | 3.119.2 (bundled by the SDK; `SKRuntimeEffect` + `SKRuntimeShaderBuilder`) |
| MVVM | CommunityToolkit.Mvvm 8.4.2 (demo page only; the card uses dependency properties) |
| Heads | `net10.0-desktop` (Skia, Win32 host), `net10.0-android` (Skia renderer), `net10.0-browserwasm` (Skia) |
| Font | Inter 4.1 static instances, SIL OFL 1.1 (`DestinationRolodex/Assets/Fonts`) |

## Build and run

```powershell
cd DestinationRolodex
dotnet build DestinationRolodex/DestinationRolodex.csproj -f net10.0-desktop -c Debug
dotnet run --project DestinationRolodex/DestinationRolodex.csproj -f net10.0-desktop
dotnet test DestinationRolodex.Tests/DestinationRolodex.Tests.csproj
```

Android: `dotnet build -f net10.0-android` then `dotnet build -f net10.0-android -t:Run` with an emulator running.
WebAssembly: `dotnet run -f net10.0-browserwasm`.

## Component API

```xml
xmlns:controls="using:DestinationRolodex.Controls"

<controls:DestinationRolodexCard x:Name="Card"
    Destinations="{x:Bind ViewModel.Destinations, Mode=OneWay}"
    SelectedIndex="{x:Bind ViewModel.SelectedIndex, Mode=TwoWay}"
    TransitionKind="MorphBlend"            <!-- MorphBlend | LiquidWarp -->
    TransitionDuration="0:0:1.2"           <!-- clamped 350–3000 ms -->
    TransitionStrength="0.6"               <!-- clamped 0–1 -->
    IsAutoPlayEnabled="True"
    AutoPlayInterval="0:0:5.5"             <!-- minimum 2.5 s -->
    IsReducedMotionEnabled="{x:Bind ViewModel.ReduceMotion, Mode=OneWay}" />
```

Read-only state: `ViewMode` (Browse/Look), `RendererStatus` (Shader / PlainImage / Unavailable), `MediaStatus`
(Empty / Loading / Ready / Failed), `IsPlaying`, `SelectedDestination`.
Methods: `Next()`, `Previous()`, `GoTo(int)`, `Play()`, `Pause()`, `ZoomIn()`, `ZoomOut()`, `ResetCamera()`, `Retry()`;
debug-only `ReplayTransitionAsync()` and `SeekTransitionAsync(double)`.
Events: `SelectionChanged`, `TransitionStarted`, `TransitionProgress`, `TransitionCompleted`, `MediaFailed`,
`RendererFallback`, `ViewModeChanged`.

`Destination` records come from `DestinationRepository.LoadPackagedAsync()` (`Assets/Data/destinations.json`, photos in
`Assets/Destinations/{id}.jpg`) or from any host-built list. Two cards on one page share nothing.

## Layout

```text
DestinationRolodex/
  Models/        Destination, FocalPoint, TransitionSettings, enums, CircularMath
  Services/      DestinationRepository (JSON → model, ms-appx photo URIs), DestinationMediaCache (async decode, LRU budget, pinning, deferred disposal)
  Rendering/     TransitionShaderSource (SkSL), PhotoRenderer (Skia draw, camera matrix, cover crop), PhotoTransitionSurface (SKCanvasElement), TransitionCoordinator, PhotoFrame
  Input/         CountryPickerController (spring, drag, wheel, keys), CameraController, Tween
  Controls/      DestinationRolodexCard, CountryPicker, GlyphLayout
  Presentation/  DestinationDemoViewModel, SpikePage (DEBUG shader diagnostic)
  Harness/       DEBUG command-line switches, FixtureExport, CaptureHelper
  Styles/        DestinationRolodexResources.xaml (tokens, fonts, icons)
tools/           capture-matrix.sh, browser_reference.py
docs/evidence/   fixtures (raw shader frames), captures (full card states), browser-reference (parity), ACCEPTANCE.md
```

## Verification harness (Debug builds)

```powershell
$exe = "DestinationRolodex/bin/Debug/net10.0-desktop/DestinationRolodex.exe"
& $exe --export-fixtures=docs/evidence/fixtures           # raw shader frames + endpoint/camera checks + report.json
& $exe --capture=out.png --viewport=390x844 --look        # full-card capture in a given state
& $exe --capture=out.png --select=jp --media-fail=jp      # failure + Retry state
& $exe --capture=out.png --steps=1000                     # 1,000-step loop; writes out.png.json with cache/memory stats
& $exe --perf=perf.json --viewport=1440x1040              # 12 s autoplay frame statistics
& $exe --spike                                            # raw photo-layer diagnostic page
bash tools/capture-matrix.sh                              # the whole acceptance matrix
python -I tools/browser_reference.py                      # browser WebGL frames with the real photos, diffed against the fixtures
```

See `SPEC.md` for decisions and `docs/evidence/ACCEPTANCE.md` for the A01–A28 record.
