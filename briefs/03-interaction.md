# 03 · Interaction brief

## One selection model, continuous visual position

Store a finite ordered destination collection and a continuous unbounded scroll position. Recycle a fixed pool of rows instead of duplicating an infinitely growing collection. The HTML uses nine rows. A logical row index maps to a destination through positive modulo. On selection, counter, description, location, credit and indicators update from one destination object.

During normal scrolling the HTML moves the picker with a damped spring. Selection changes when rounded continuous position changes. The photograph transition starts when the next photo is ready. This means photo transition progress is not inherently the same as spring progress. During **debug replay/scrubbing only**, the HTML synchronizes picker position to smoothstep(progress). Do not accidentally make normal wheel navigation depend on the debug timeline.

Use floor(position + 0.5) to reproduce JavaScript Math.round, including negative half values; C# Math.Round uses a different default tie rule. See `uno-reference/models/CircularMath.cs`.

## Input contract

| Input/context | Result |
| --- | --- |
| Wheel over scene, browsing | Move continuous target; normalize device delta; snap after 110 ms idle |
| Vertical drag/swipe | Follow displacement, then settle with bounded release momentum |
| Click an adjacent country | Move to that logical row |
| Previous/next controls | One step; wrap at both ends |
| Position indicator | Shortest path to that destination; tie resolves forward |
| Up/down with picker focus | One step |
| Page up/down | Three steps |
| Home/end | First/last country |
| Country-letter typing | Type-ahead search; reset after 650 ms |
| Horizontal dominant gesture | Do not consume as vertical selection |
| Modified wheel / browser zoom | Do not consume as country input |
| Any manual country navigation | Pause autoplay and exit timeline inspection |

Prototype browser wheel scaling is delta/115 with a per-event ±180 clamp. Uno devices report wheel units differently; normalize first and tune equivalent perceived movement rather than copying raw deltas. Pointer moves under 5 px remain click candidates. Handle capture loss and cancellation; never leave the card stuck dragging.

## Row appearance

For distance `d = logicalIndex - position`, use `a = abs(d)`:

```text
Y = d × rowPitch × (1 - min(a,3) × .035)
Z = -a × 13
RotateX = -d × 12 degrees
Scale = max(.65, 1 - a × .044)
Opacity = 1 - .5a                     for a ≤ 1
          .5 - .34(a-1)              for 1 < a ≤ 2
          .16(3-a)                    otherwise
```

Clamp opacity to [0,1], hide rows at distance ≥3, suppress duplicates for a single destination. The HTML applies up to 1 px blur to distant neighbors. If a platform cannot reproduce this blur cheaply, keep scale, opacity and row positions correct first and record the delta. Do not rasterize the whole interactive text layer to chase the blur.

## Spring and camera reference

Normal picker acceleration is `(target-position)*200 - velocity*27`; timestep is capped at .032 seconds. Integrate velocity, then position. For a stronger frame-rate-independent Uno implementation, use a fixed timestep/accumulator or an equivalent analytic spring and compare the resulting settle curve. Preserve perceptual timing, not dropped-frame bugs.

Camera damping uses `1-exp(-dt*6)`. Normal pointer displacement is ±12 logical pixels; Look around uses ±37. Zoom is clamped to 1.0–1.65. Normal perspective tilt is approximately ±1.25° X / ±1.6° Y; Look around permits ±2.8° X / ±3.8° Y. The 13% overscan hides image edges. The camera and the shader must use consistent cover/focal mapping at every endpoint.

## Look around

Entering pauses autoplay, exits debug preview, hides and disables the picker, reveals the country title and camera controls, and resets the camera. The selected country does not change. Drag pans; wheel changes zoom; plus/minus buttons zoom by .15; Reset restores pan and 1.0× zoom. Arrow keys pan when the photo view owns focus. Escape restores browsing and returns focus to the picker. The image is flat: do not imply an actual orbit around hidden geometry.

## Autoplay and lifecycle

The demo auto interval is 5500 ms with a minimum configurable interval of 2500 ms. Passive hover/focus pauses automatic advance; an explicit Play activation can continue while the button retains focus. Any manual selection pauses it. Progress is reset on manual change. Pause background/occluded work and detach callbacks/resources on unload. On resume, continue safely rather than skipping several destinations.

When reduced motion is enabled, disable procedural transitions, passive parallax, ambient drift and default autoplay; use immediate state updates. If the user explicitly requests play, keep the destination changes discrete without re-enabling decorative motion. Look-around pan/zoom may remain directly user-controlled without inertial tilt.

## Media state and rapid input

**Observed HTML:** stale async loads use generation tokens; only the latest can mount. The previous full photo may remain visible while another destination loads, and an interrupted effect is not guaranteed to preserve the exact rendered intermediate frame.

**Proposed Uno requirements:** keep selection immediately responsive; expose loading state; never silently label a stale photograph as the selected one. While waiting, use the selected destination's own preview or an explicit neutral loading surface. Queue only the latest ready target, never an unbounded chain. During an active effect, either finish that bounded transition then run the latest target, or snapshot/retarget the current composite. Start with the bounded latest-target policy; only add snapshot retargeting if the supported rendering backend permits it without expensive per-frame CPU readback. The visible media identity and loading state must remain explicit.

A render failure uses a plain image/short crossfade capability path. It is not an approved third effect and must not masquerade as Morph Blend. A decode failure offers Retry. Empty collections display a deliberate empty state; one item disables navigation/autoplay. Replacement data invalidates old work and preserves selection by ID when possible.

## Accessibility

Country text and buttons remain genuine XAML elements with names and meaningful focus order. Expose a single selection surface and the selected value rather than nine confusing duplicate accessible options. Keep fallback previous/next controls available. Announce settled manual changes, not every frame. Use polite status for loading/errors, preserve keyboard activation, and ensure text scaling, high contrast and touch hit regions are tested. Motion reduction is not equivalent to disabling access to content.
