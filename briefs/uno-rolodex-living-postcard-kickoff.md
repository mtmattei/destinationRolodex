# Agent kickoff: Uno Destination Rolodex / Living Postcard

Read `destination-rolodex-uno-living-postcard-brief.md` first. This is revision 2 of the Uno build brief. Its current source of truth is `reference/destination-rolodex-living-postcard.html`, not the original split-panel card or older generated image concepts. Extract `reference/destination-rolodex-living-postcard-source.zip` and read the component, both photo shaders, geographic globe, destination data, and Living Postcard notes. Follow the target repository's instructions and use its existing Uno conventions.

Implement a reusable Uno component with one photographic surface behind the selector, integrated caption, and overlaid controls. Preserve the 6-unit warm rim, understated shadow/highlight, sans-serif country / serif place hierarchy, measured caption layout, clickable dots, circular arrows, and synchronized geographic globe. No numeric counter, in-photo effect badge, Orbit Depth, or Slice Shift.

One continuous circular position must drive row placement, photo shader progress, globe orientation, photographic text overlay, material colors, and arrival treatment. Do not start a separate image timer on SelectedIndex changes. Reverse a gesture by rewinding the same ordered pair. At arrival, photo-in-type and distortion clear completely; type and caption are crisp. Keep a readable solid text base under the photographic overlay and maintain native/accessibility semantics.

Inspect the existing solution and record target heads, renderer, and resolved package versions. C#/XAML/MVVM and compatible .NET 10 are fresh-project assumptions, not permission to rewrite an existing app. Windows Skia Desktop plus Android is a proposed first test order, not a finalized shipping matrix.

First prove a small compositing spike: both effects, translucent type, photo-filled letters, integrated caption scrim, globe, rounded clipping, focus, and touch. The recommended native route is XAML/C# with an integrated Skia surface where supported. Check SKCanvasElement support before construction and compile against installed APIs; the old SkSL candidate was not validated as native code.

The old blanket WebView prohibition is superseded. A complete-card WebView is a permitted option to assess, not a settled decision. If selected, keep all overlapping postcard content, including the caption, within the web composition. Uno owns surrounding application UI/data. Bundle local content, use a small validated command/event bridge, and keep animation frames inside the web renderer. Do not silently change the renderer or split the caption back into a white panel.

Preserve wheel/drag/keyboard/type-ahead, infinite wrap, Look around/pan/zoom/reset, explicit tour Play/Pause, and the separate comparison lab with Morph Blend, Liquid Warp, replay/seek, and diagnostic crossfade. Tour starts paused. No idle drift, spinning globe, pulsing, or light loops. Reduced motion must work at startup and during interruption.

Follow the six build stages and acceptance criteria LP01–LP20 in the brief. Use cancellation generations, explicit displayed-media identity, bounded caches, stable elapsed-time spring integration, instance-local state, and lifecycle cleanup. Do not let late photos win or broadcast broad MVVM updates every frame.

The six full-size photographs still need acquisition, visual inspection, rights review, and offline packaging. Preserve the latest coordinates, pigments, focal points, captions, and credits. No substitute test imagery may be presented as final destination assets.

Deliver build/run instructions, the renderer decision, actual tested configurations, source changes, component API usage, real recordings and fixed-progress captures, accessibility/failure results, measured performance, and precise remaining gates. Historical browser checks are not native test passes. Do not stop at a screenshot or a successful compile.
