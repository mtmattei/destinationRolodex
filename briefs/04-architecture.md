# 04 · Architecture brief

## Recommended shape

Use a single Uno Platform app with XAML layout and C# logic, MVVM for observable selection/configuration, and a small renderer-specific surface for the photographs. Do not wrap the HTML in WebView and call the rebuild done. Do not redraw the entire interface on a Skia canvas.

The candidate path is **SKCanvasElement + SkiaSharp SKRuntimeEffect**. Uno documents a custom RenderOverride(SKCanvas, Size), explicit Invalidate(), and the support check IsSupportedOnCurrentPlatform(). It also says construction on unsupported configurations throws. The rendering/package support depends on the selected target and renderer; the documentation includes a Windows App SDK section alongside Skia-only support conditions. Treat that as a capability gate rather than assuming universal support. [S1]

The current template documentation exposes .NET 10, XAML and MVVM settings. Choose the current compatible Uno template/SDK on the build machine, record the resolved versions, and preserve a lockfile. Do not invent a latest patch number or manually mix unrelated SkiaSharp native assets. [S4]

## Target decision before UI construction

| Target proposal | Required spike evidence |
| --- | --- |
| Skia Desktop on Windows | Surface support, shader compile, correct two-photo endpoints, actual frame timings |
| Android with Skia renderer | Same output on emulator and intended physical device; lifecycle/resource behavior |
| WebAssembly with Skia renderer | Browser decode, packaged media, runtime-effect support and render cost |
| Windows App SDK / native WinUI | Separate support/build architecture check; do not equate with Skia Desktop on Windows |
| iOS/macOS | Separate build-host and device validation before claiming support |

Check `SKCanvasElement.IsSupportedOnCurrentPlatform()` before construction and lazily create the supported surface. Do not declare it unconditionally in shared XAML when doing so can throw on an unsupported target. If support fails, show the explicit fallback and resolve the platform requirement before proceeding. Rendering an image is not proof that runtime effects are supported or fast enough.

## Ownership boundaries

| Part | Owns | Does not own |
| --- | --- | --- |
| Destination model/repository | IDs, country/location/copy, asset path, focal point, attribution | Transient animation frames |
| Card view model | Selected ID/index, transition kind/settings, browse/look, autoplay, media status, commands | SKImage/GPU objects or per-frame PropertyChanged |
| Picker controller | Continuous position, velocity, target, row recycling, device-independent gesture state | Photo decoding |
| Transition coordinator | Source/target identity, cancellation generation, progress clock, latest-target policy | XAML page layout |
| Media cache | Async decode, bounded cache, resource leases, error/retry | Country selection authority |
| Photo surface | Cover sampling, runtime shader, camera, invalidation, resource disposal | Buttons, labels, network calls in draw |
| Demo view model | Effect comparison, timeline, test pair, diagnostics | Production navigation or business state |

A frame loop can update a lightweight render-state object and invalidate the surface. Avoid notifying the whole MVVM tree for every shader progress tick. The view model receives meaningful state changes; animation remains an implementation detail of the component.

## Suggested project layout

```text
DestinationRolodex/
  Models/Destination.cs
  ViewModels/DestinationCardViewModel.cs
  Controls/DestinationRolodexCard.xaml[.cs]
  Controls/CountryPicker.xaml[.cs]
  Rendering/PhotoTransitionSurface.cs
  Rendering/TransitionCoordinator.cs
  Rendering/TransitionParameters.cs
  Input/CountryPickerController.cs
  Services/DestinationRepository.cs
  Services/DestinationMediaCache.cs
  Styles/DestinationRolodexResources.xaml
  Assets/Destinations/{ph,jp,it,no,is,nz}.jpg
  Assets/Data/destinations.json
  Shaders/DestinationTransitions.sksl
  Presentation/DestinationDemoPage.xaml[.cs]
  Tests/{Unit,Integration,Visual}/
```

These are proposed application-owned class names, not Uno APIs. Only the verified platform/library APIs referenced in Sources should be assumed to exist.

## Resource and rendering rules

Decode asynchronously outside RenderOverride; hand safe references to the render thread. Cache compiled effect(s), decoded photos and reusable paints. Mutate uniforms without re-parsing SkSL each frame. Create immutable frame inputs or otherwise synchronize read/write ownership. Dispose child shaders, effects, image handles and buffers according to their actual ownership and the installed API. Never dispose an SKImage while an active frame or queued transition still references it.

Keep the active source and target resident; retain immediate neighbors opportunistically within a configurable budget. Do not copy the HTML's unbounded URL cache into a reusable control for arbitrary catalogs. Reinitialize resources on context/device loss and detach observers/frame callbacks when the card unloads. Offscreen cards must not render indefinitely.

Uno performance guidance recommends the integrated SKCanvasElement path over SKXamlCanvas for Skia targets and documents frame-rate diagnostics. Use those diagnostics and device traces; do not assume a target 60 fps is achieved because a frame callback runs 60 times per second. [S5]

## GLSL → SkSL boundary

The existing shader source is retained in `uno-reference/shaders/reference.frag.glsl`. `DestinationTransitions.sksl` is a hand-ported candidate. SkSL samples child shaders through `.eval()` and uses image pixel coordinates rather than normalized GL texture coordinates; it also participates in Skia color management and premultiplied-alpha handling. Those differences require explicit conversion and visual checks. [S2]

Bind the two image children, dimensions, cover/focal parameters, progress, intensity, direction and mode. The C# API family is documented under SKRuntimeEffect; exact overloads must be resolved against the installed package version. [S3] The handoff does not claim the SkSL candidate or C# references were compiled.

## Data and offline policy

The HTML uses online image URLs. The Uno rebuild should use packaged files for the six demo destinations after `tools/fetch_assets.py` succeeds. `assets/manifest.json` declares their intended ms-appx URIs. The reference/preview does not stand in for a full-size licensed image at release. All six entries must decode before the offline milestone passes.

Do not create auth/API/service dependencies for this component. Repository injection makes future catalog changes possible without requiring a backend in the demo.

Sources: `briefs/09-sources.md`, especially S1–S5.
