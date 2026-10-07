# Uno implementation references, not a runnable app

No .NET SDK is installed in the packaging environment. These C# and XAML files have **not** been compiled, and the SkSL candidate has **not** been executed in SkiaSharp/Uno. Use them to reduce transcription work, then compile and test on your actual target. The included browser renderer is the executable reference.

`models/Destination.cs` provides plain data contracts. `CircularMath.cs` preserves positive modulo and JavaScript-compatible rounding, and exposes reference row geometry. These helpers do not create a UI. `resources/DestinationRolodexResources.xaml` seeds stable named colors and sizes; it does not implement the card.

`shaders/reference.frag.glsl` and `reference.vert.glsl` are extracted from the approved live renderer. `DestinationTransitions.sksl` is a conversion candidate with explicit Skia coordinate handling. `shaders/UNIFORMS.md` describes binding and coordinate rules. Compile the effect before polishing the surrounding card.

## Proposed component API

The following is a design contract for an application-owned control, not an assertion that Uno ships this control:

```text
Destinations: IReadOnlyList<Destination>
SelectedIndex / SelectedDestinationId
TransitionKind: MorphBlend | LiquidWarp
TransitionDuration: TimeSpan (default 1200ms)
TransitionStrength: double (default .60, range 0..1)
IsAutoPlayEnabled / AutoPlayInterval (demo: 5500ms)
IsReducedMotionEnabled (OS policy + application setting)
ViewMode: Browse | Look
RendererStatus / MediaStatus
Commands: Next, Previous, Select, Play, Pause, LookAround, ResetCamera, Retry
Events: SelectionChanged, TransitionStarted, TransitionCompleted, MediaFailed, RendererFallback
```

Debug-only operations: replay the last pair and seek fixed progress. The visual frame's position/progress is not a general MVVM property-change stream. A reusable control can expose dependency properties for host binding while delegating animation/input to internal controllers. Integrate with the host app's established architecture rather than creating a global singleton.

Recommended XAML hierarchy: rounded card container → clipped scene with lazily created supported photo surface → real country text, markers and controls → separate white details region. Lazily create the graphics surface after checking platform support; do not put an unsupported surface directly in shared XAML and discover the error during page construction.
