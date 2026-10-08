# Destination Rolodex
## Uno Platform build brief · Living Postcard

**Revision 2 · 8 October 2026**  
**Visual authority:** Living Postcard HTML, browser revision 05  
**Status:** Updated implementation specification. The Uno renderer and shipping target matrix are not yet finalized.

> Build one continuous photographic card whose country selector, image transition, geographic globe, and atmosphere respond to the same movement. The card should invite exploration, respond immediately, and become completely still and crisp when the user arrives.

## 1. Scope and source of truth

Rebuild the latest working Living Postcard component inside an Uno Platform application. This is an implementation update, not another redesign. Preserve the approved composition and behavior while adding the platform-specific accessibility, resource management, and failure handling needed for a reusable control.

Use `reference/destination-rolodex-living-postcard.html` and `reference/destination-rolodex-living-postcard-source.zip` in this brief pack. The editable source contains `rolodex.js`, `transition-engine.js`, `globe.js`, `destinations.json`, and implementation notes under `docs/`. Inspect the live prototype, not only its screenshots. [R1–R3]

**Precedence:** this brief and the latest Living Postcard source supersede conflicting instructions in the original Uno handoff. Older screenshots, `approved.html`, design tokens, the static globe SVG, state models, and the original agent kickoff are historical references, not current design authority. The earlier blanket “no WebView” rule is withdrawn; a bounded web component is an option to evaluate, not an already-approved architectural choice.

### What has changed since the original Uno brief

| Area | Current requirement |
| --- | --- |
| Card structure | One photograph continues behind the caption and controls. No detached white information panel. |
| Material and typography | Warm 6-unit print rim, subtle edge highlight, destination-tinted shadow, large sans-serif country names, serif place caption. |
| Scene motion | Continuous picker position drives image, names, globe, and material colors. No second photo timer during browsing. |
| Moving type | A restrained destination-photo fill overlays readable text in motion; it clears completely on arrival. No glyph deformation. |
| Globe | Geographic orthographic globe oriented toward the featured place, with shortest-arc interpolation. Not a rotating flat icon. |
| Controls | Quiet dots and circular navigation buttons; remove `01 / 06` and the on-image transition badge. |
| Playback | Tour starts paused. No autonomous camera drift, globe spin, pulsing, or lighting loop. |
| Rendering decision | Compare native Skia composition with a complete-card WebView only as needed. An image-only WebView is not the default boundary. |

Only **Morph Blend** and **Liquid Warp** are selectable production effects. Keep ordinary image/crossfade fallback for reliability, and Original Crossfade only in the development comparison surface. Do not restore Orbit Depth or Slice Shift. The Look around icon is unrelated to the removed Orbit Depth effect and remains.

Do not add booking, accounts, maps, saved destinations, travel APIs, or new copy. Honor an existing Uno solution and its conventions. For a fresh project, the working assumption is C#, XAML, MVVM, and a compatible .NET 10 setup. Record resolved SDK/package versions instead of inventing a “latest” version or upgrading unrelated dependencies.

## 2. Visual composition

The intended mood is **curiosity, warmth, and quiet wonder**. The scene is the focal point; decorative details and technical controls remain secondary.

The photograph fills the complete inner card, including the lower caption. Add a feathered dark scrim that becomes strongest behind the caption without creating a hard upper boundary. Keep the upper photograph’s natural colors rather than tinting the whole scene. The warm rim and fine inner highlight give the object depth without a large glass panel. [R1, R2]

The selected country is centered in the available browsing area above the caption, not at the mathematical center of the complete card. Faded neighboring names reveal the infinite vertical list. The top-left globe and “DESTINATIONS” label balance the top-right Play/Pause control. The gesture hint and Look around action sit above the integrated caption. Location, region, description, pagination, and previous/next remain inside the photograph.

### Reference measurements

Values below come from the latest browser source. Treat CSS pixels as initial device-independent layout values, then validate text metrics and density in Uno. They are not physical-pixel instructions. [R1, R3]

| Element | Reference target |
| --- | --- |
| Width and rim | Maximum width 480; rim 6; outer radius 30, or 27 in the mobile demo. Inner radius equals outer radius minus rim. |
| Height | Desktop preferred scene height `clamp(580, 0.74 × viewportHeight, 670)`, subject to measured content minimums. |
| Compact height | Mobile demo uses `clamp(553, 0.73 × viewportHeight, 620)`; minimum room for content takes precedence. |
| Country type | Sans-serif, weight about 650, tracking about −0.055 em; reference size `clamp(34, 0.1235 × cardWidth, 61)`. Fit longer labels without clipping. |
| Place caption | Georgia/serif character, 23 desktop and roughly 20–21 compact; regular weight and restrained negative tracking. |
| Description | Sans-serif 12, line height about 1.7; source color `#EEEADE`. Native scaled-text readability takes precedence over a literal size. |
| Globe and buttons | Globe 27, or 25 compact; main visible button circles 40. Enlarge native hit regions without crowding the layout. |
| Caption inset | About 26 horizontally and 19 below; compact layouts use 18–22 horizontally. |
| Pagination | Unselected dot 4 × 4; selected mark 21 × 3; buttons remain individually reachable. |
| Base palette | Page `#F4F1EB`; rim `#EBE6DC`; selected country `#FFFDF6`; caption `#FFF8EB`. |

**Layout must be content-aware.** Measure the caption, then reserve its height plus the scene-footer clearance before placing the picker. The prototype’s scene minimum is measured caption height plus 400; compact styles must still protect the list and controls. Long captions and larger text grow the card rather than overlapping or truncating content. A host page may scroll vertically. Never shrink essential text solely to force the entire card into a short viewport.

Keep the serif/sans-serif contrast, but resolve actual fonts and weights against the target app. Browser fallback fonts are not guaranteed to exist on each target. No font files are supplied. Record any substitution and compare line breaks, selected-country width, and caption height.

## 3. One shared interaction state

**This is the core implementation requirement.** Do not animate the list and then start a separate photo transition when `SelectedIndex` changes.

Maintain an unbounded, fractional picker position `q`, a target position, and velocity. For `N > 0`:

```text
low          = floor(q)
p            = q - low                    // 0 <= p < 1
fromIndex    = positiveModulo(low, N)
toIndex      = positiveModulo(low + 1, N)
selected     = positiveModulo(floor(q + 0.5), N)
travel       = sin(PI * p)                 // zero at arrival, peak mid-flight
```

Use positive modulo and the stated rounding expression in C#. Preserve behavior at negative positions and half steps. Rebase large positions by whole multiples of `N` without changing the displayed pair, drag origin, or visual state.

One frame sample drives country positions and opacity, photo uniforms, globe orientation, photo-in-type amount, caption treatment, material colors, and the subtle arrival camera treatment. Optional pointer parallax is a separate input to that same frame, not another destination-selection authority. [R3]

During reverse movement, decrease `p` within the same ordered adjacent pair. Do not swap texture identities or flip the liquid origin merely because pointer direction changed. The browser’s interactive path uses a canonical ordered pair and fixed shader direction; reversing the fraction rewinds the effect. Transition direction in a timed comparison is a separate concern.

A finite pool of nine rows is the reference implementation; do not allocate an ever-growing list. Direct drag updates `q`. Wheel, keyboard, dots, and arrow buttons update the controller target. Frame updates must not notify the entire MVVM tree.

**Arrival contract:** as `q` reaches the selected integer, distortion and photographic text fill become zero; selected type becomes solid; caption opacity returns to 1 and displacement to 0; globe and material match that destination; the correct full-resolution resting image replaces any transient shader surface without a visible crop jump. Render work stops once camera/selection motion has settled and the tour is paused.

### Interrupted input and media ownership

An input reversal must take effect immediately. New navigation cancels obsolete replay/seek work. Use generation IDs or cancellation tokens for image decode and pair preparation. A late completion may populate a cache but must never replace a newer visible destination. When a pair finishes loading, draw the current fraction, not the fraction from request time.

Separate **selection candidate**, **settled destination**, and **displayed media identity**. A nearest-row candidate may change at the half step as in the prototype; a settled event fires only after arrival. Comparison events must be marked as previews so a replay cannot accidentally trigger app navigation or persistence.

The latest-target/cancellation model from the old handoff remains useful for asynchronous resources. It must not replace gesture-linked adjacent-pair rendering with a queue of independent timed transitions.

## 4. Photo transitions, text, and atmosphere

Keep the two real procedural effects from `transition-engine.js`. Morph Blend distorts both photographs through a shared flow field and blends them. Liquid Warp reveals the next image through an irregular expanding refractive boundary. Neither recognizes landmarks, reconstructs geometry, or performs optical flow. [R1, R2]

**Interactive timing is position-driven.** The inherited 1,200 ms duration and 0.60 strength are the current defaults, not newly approved tuning. Duration governs timed replay, not how long a user must wait after dragging. The external lab retains the 500–2,400 ms duration range, intensity, replay, and normalized scrub controls.

**Photo-in-type:** each moving row uses its own destination’s loaded photograph or preview as an overlay clipped to its letters, above a readable solid text base. Its reference opacity is `travel × 0.60 × max(0, 1 − abs(rowDistance) × 0.72)`. It is exactly zero at integer arrival. This is not a cutout through the scene or a deformation of letter shapes. On a native path, use a cached, correctly shaped text mask or equivalent render treatment; do not rasterize the whole card to simulate it. Text must retain accessible semantics. [R2, R3]

**Material colors:** interpolate the two authored `destination.color` values using `p`. Derive caption ink as 85% neutral RGB `(24,25,23)` plus 15% destination pigment; derive the accent as 80% warm RGB `(255,247,224)` plus 20% pigment. The same mixture influences the ambient shadow. This is authored color interpolation, not automatic sampling of the photograph. Preserve these low-saturation relationships. [R3]

### Motion targets from the reference

| Response | Initial Uno target |
| --- | --- |
| Entrance | Once per presentation: 560 ms; 8-unit rise; scale 0.996 → 1; easing equivalent to `(0.16, 1, 0.3, 1)`. |
| Hover | Fine pointers only: 2-unit lift, 420 ms settling; cancel the lift during drag. |
| Pointer lighting | Faint warm highlight at the pointer, no idle cycle; opacity response about 320 ms. |
| Press | 0.94–0.97 scale depending on control; about 90 ms down and 240 ms release. |
| Caption in flight | At most 17% opacity reduction and 2-unit downward movement, controlled by `travel`. |
| Browse camera | Up to 10 units pointer pan; subtle tilt. Arrival adds up to 1.2% scale and 2 units vertical travel. |
| Look around | Constrained flat-image pan; reference amplitude 37; zoom 1.0–1.65. No invented 360° geometry. |
| Settle spring | Reference acceleration `115 × (target − q) − 21 × velocity`; integrate elapsed time in substeps of 1/120 s or smaller. |

Use a monotonic clock and reset it after suspension. The browser bounds elapsed catch-up to 0.5 seconds. Do not slow all motion down merely because frames are dropped, and do not run expensive catch-up work after a long background period. These values are fidelity starting points, not a native performance guarantee.

## 5. Geographic globe

Reproduce the location-aware globe in `globe.js`, not the old static SVG. It uses an orthographic Earth projection with a small center marker and no idle spin. Its camera follows the same adjacent pair and fraction as the scene. Interpolate unit vectors along the shortest great-circle arc, including across the date line. Handle nearly identical and antipodal positions deterministically. [R2, R3]

The coordinates represent the featured place rather than a country centroid. Preserve this source data; validate the actual photograph separately before release.

| ID / country | Featured place | Latitude, longitude | Authored pigment |
| --- | --- | --- | --- |
| ph / Philippines | Coron, Palawan | 11.98, 120.20 | `#166A73` |
| jp / Japan | Mount Fuji, Yamanashi | 35.36, 138.73 | `#607F98` |
| it / Italy | Lago di Braies, Dolomites | 46.69, 12.09 | `#397779` |
| no / Norway | Lofoten Islands | 68.21, 13.61 | `#426578` |
| is / Iceland | Skógafoss, South Coast | 63.53, −19.51 | `#526E60` |
| nz / New Zealand | Milford Sound, Fiordland | −44.67, 167.93 | `#425A53` |

The source includes a local 512 × 256 coastline bitmask and a 96 × 96 globe render buffer, displayed at 25–27 units. Keep it local and lightweight. Natural Earth’s terms place its raster and vector data in the public domain; retain the supplied provenance note. [S6]

**Native hardening beyond the prototype:** a selected destination without coordinates must show a neutral, unpinned globe. Do not inherit a neighbor’s pin when only one endpoint has coordinates. Add tests for missing/invalid coordinates, the date line, the poles, and antipodal pairs. The globe is not an interactive map and does not need a location-permission request.

## 6. Rendering architecture and decision gate

**Recommended default: Uno XAML/C# with integrated Skia drawing on compatible targets.** Keep the control API, semantic text, input, and application integration native. Use one coordinated drawing path for photograph effects and any custom image-in-type/globe rendering that XAML alone cannot reproduce faithfully.

On Skia-rendered Uno targets, `SKCanvasElement` can use the existing application canvas. The official documentation requires checking `IsSupportedOnCurrentPlatform()` and describes explicit `Invalidate()` calls. Verify support before construction; the selected Windows head and renderer matter. Do not equate Windows Skia Desktop with Windows App SDK. [S1]

The first native spike must prove more than photo morphing: two photos, readable semi-transparent text, a photo-filled text mask, the integrated caption scrim, rounded clipping, and the globe on the same visual composition. Cache text shaping/masks and compiled effects. Resolve actual APIs against installed packages. The old SkSL port remains an **uncompiled candidate**, not production code. SkSL image coordinates, child-shader sampling, and color/alpha behavior need explicit translation from GLSL. [S2]

### Permitted alternative: a complete postcard in WebView2

The prior discussion did not finalize this choice. A WebView prototype is valid when reusing the current HTML offers a better fidelity/delivery tradeoff. It is not permission to replace the whole Uno app with a web page.

**Updated boundary:** if chosen, keep the photograph, country rows, photographic text, globe, scrim, integrated caption, overlaid controls, lighting, and frame treatment within one web composition. Uno owns surrounding layout, destination data, app navigation, and the host-facing API. Do not put the caption back into a detached native panel merely to simplify the bridge.

An image-only WebView beneath translucent XAML is not the default. Uno documents alpha-blending and transformed-clipping limitations when native content is embedded in a Skia app. Verify the exact host rather than assuming all overlaid native/XAML content will blend. [S3]

Bundle web content and assets locally. Uno documents packaged-content mapping and JavaScript/C# messages; underlying engines differ by platform, including Android WebView, Apple WebKit, and an iframe on WebAssembly. Validate each chosen target. [S4]

Keep gesture handling and frame-by-frame animation inside the WebView. Exchange bounded commands and semantic events, not per-frame C# calls. Validate message schemas, IDs, ranges, and allowed navigation; do not expose arbitrary file/script access. Microsoft’s guidance advises reducing host/web communication and measuring startup and memory overhead. [S5]

**Gate before the full build:** compare the same two photos and caption using the proposed rendering path. Record compositing correctness, clip behavior, input/focus, accessible text, resize behavior, startup, memory, and actual frame presentation. Implement one selected production path after this gate; do not build two complete controls without a concrete need. Document the renderer choice and any accepted deviation.

## 7. Uno component boundaries

The names below are proposed application-owned types, not claimed Uno APIs.

| Part | Responsibility |
| --- | --- |
| `DestinationRolodexCard` | Public reusable control, layout, commands, focus, and accessible state. |
| `DestinationCardViewModel` | Destination catalog, candidate/settled selection, view mode, effect settings, playback, media status. |
| `PickerController` | Continuous position, target, velocity, gesture routing, wrapping, and stable settling. |
| `SceneCoordinator` | One frame snapshot, adjacent pair, cancellation generation, preview mode, and arrival state. |
| Rendering implementation | Photo effect, crop, camera, text treatment, globe/material output; or the approved complete-card web adapter. |
| `DestinationMediaCache` | Async acquisition/decode, bounded resource leases, retries, and disposal. |
| Demo settings view | Mode comparison, replay pair, timeline, diagnostics, and no-shader testing. Separate from production UI. |

Keep public settings such as `ItemsSource`, `SelectedDestinationId`, `TransitionKind`, `TransitionStrength`, `ReplayDuration`, `ViewMode`, `IsPlaying`, and motion preference. Provide next/previous/select, play/pause, enter/exit Look around, reset camera, and development replay/seek. Expose selection/settled, view, readiness, fallback, and error events. Preview events carry a preview flag.

A frame snapshot includes `q`, pair IDs, `p`, gesture direction for camera response, effect kind/strength, camera state, globe orientation, material values, and resource generation. Apply that snapshot coherently. Frame-local state stays outside broad MVVM notifications. Two card instances must work independently.

For a web implementation, native code owns the catalog and meaningful app selection; the embedded component owns transient gesture state. Use instance IDs and a versioned message contract. Restore selection and settings after view recreation without replaying an entrance or auto-starting the tour unintentionally.

## 8. Inputs, playback, and development controls

Preserve vertical wheel/trackpad scrolling, vertical touch/mouse drag, clicking a visible country, clickable pagination, and previous/next. Capture input only after a browse gesture is recognized and release capture on cancel. Do not let a control press start a drag. Keep surrounding-page scrolling usable outside the scene.

The focused picker supports Up/Down, Page Up/Down, Home/End, and type-ahead. Look around hides and removes the picker from focus/hit testing; drag pans, wheel or plus/minus zooms, reset restores the camera, and Escape returns focus to browsing. Pointer parallax is not device-orientation access.

Tour playback starts **paused**. Play advances every 5,500 ms by default; manual browse and entering Look around pause it. Hidden/offscreen/unloaded cards stop render work and reset timing. No idle camera or globe movement should continue just because the card is visible.

The development panel retains Morph Blend/Liquid Warp, strength, replay duration, replay of the same pair, scrub endpoints, fallback status, and Original Crossfade comparison. Mode shortcuts 1/2 and replay shortcut R must not override typing or picker keys. Normal browsing exits timeline inspection cleanly. Keep this panel outside the production postcard.

## 9. Assets, failures, and accessibility

### Media and resource policy

Use the six entries from the **latest** `destinations.json`, including `geo`, pigments, copy, focal positions, alt text, credits, and source URLs. An example is included as `data/destinations.json` in this pack; paths are still the browser source paths, not already-configured Uno asset URIs.

The provided source bundles only the Philippines loading preview, not the six full-size photographs. Fetch, inspect, and package the final photos for the offline Uno demo. Retain attribution and validate distribution rights. Do not present reference crops or synthetic test fixtures as cleared final destination assets. The gallery must not rely on a remote image host to work at first launch. [R2, R3]

Decode outside rendering, compile effects once, reuse paints/masks, and bound image/texture caches. Retain active pair resources until no frame references them. Release callbacks, handlers, timers, and owned GPU/browser resources on unload. Avoid image readback for colors and per-frame string/bitmap allocation.

The browser’s transient shader limits are 1.25× device pixel ratio, 900,000 render pixels, and a 1,600-pixel longest texture edge; its resting image remains full resolution. Treat these as reference quality controls to profile, not universal native limits. Match endpoint crop and color before changing render quality.

### Explicit states

Support loading, preview, ready, image error with Retry, shader unavailable, reduced motion, suspended, empty catalog, and single destination. A media failure must never silently label an old photograph as a new place. Use a matching preview or clearly labeled neutral fallback. Cache failed pair attempts and retry deliberately rather than on every animation frame. Empty/single catalogs must not divide by zero or expose meaningless navigation.

### Accessible equivalents

Maintain one semantic country list and selected state; decorative photographic text/globe layers must not produce duplicate announcements. Announce settled changes, not every frame. Keep a visible focus indicator and a logical reading order through location, description, and controls. Native hit regions should target 44–48 units where feasible; reflow controls rather than overlapping hit regions.

Validate ordinary text at 4.5:1, large text at 3:1, and necessary control/focus boundaries at 3:1. Check actual image backgrounds and faded interactive names, not only token colors. These are accessibility acceptance targets, not a claim that the prototype already passes an audit. [S7, S8]

Use OS reduced-motion settings plus an app override. Remove entrance, lift, parallax/tilt, photo displacement, moving photo fill, and animated globe travel. Selection and orientation update immediately; caption remains fully opaque; destination color is static. A change to reduced motion during a gesture cancels the active frame treatment and settles safely. Preserve deliberate pan/zoom without animated interpolation. Stop the tour on preference change; explicit Play may use discrete changes without motion. High-contrast settings replace photographic text/readability treatments with dependable system-color surfaces.

## 10. Implementation sequence

**1 — Baseline and capability spike.** Inspect the existing solution, latest prototype, source notes, and actual dependencies. Establish target heads. Proposed first validation targets are Windows Skia Desktop and Android; these are not an agreed shipping matrix. Prove both shaders, text/photo composition, clipping, and globe. Record the renderer decision before committing to the full control.

**2 — Static postcard and assets.** Build the content-measured integrated card, resolve typography, package approved photos, and implement semantic text/controls. Validate desktop, compact, long-caption, and scaled-text layouts before adding subtle motion.

**3 — Shared controller.** Implement positive modulo, compatible rounding, recycled rows, monotonic spring integration, candidate/settled identities, and a testable frame snapshot. Add normal inputs, reversal, loop boundaries, and rapid navigation.

**4 — Complete scene.** Connect both effects, globe, photographic text, material blend, caption arrival, and camera to the same frame. Match fixed progress in both directions. Add generation-safe media readiness and clean full-resolution arrival.

**5 — Interaction and lifecycle.** Restore Look around, focus, tour, replay/seek lab, tactile controls, reduced motion, high contrast, empty/error states, and disposal. Test two independent instances.

**6 — Native validation and delivery.** Capture real-device recordings and metrics, compare to the reference, document accepted differences, and resolve asset/renderer gates. Compilation or a still screenshot alone is not completion.

## 11. Acceptance and evidence

Each criterion needs a pass/fail result, target/configuration, and evidence location. Do not carry forward earlier browser pass counts as native test results.

| ID | Required evidence |
| --- | --- |
| LP01 | One continuous image behind the integrated caption, controls, and feathered scrim. No white information panel. |
| LP02 | Warm rim, correct type hierarchy, quiet dots/circular buttons; no numerical counter or in-photo mode badge. |
| LP03 | Slow drag at 25%, 50%, and 75% shows photo, names, globe, and colors sampling one frame. |
| LP04 | Reverse at mid-transition without starting a new animation, swapping the image pair, or jumping the liquid origin. |
| LP05 | Both effects produce genuine displacement/refraction and clean endpoints. Test both directions. |
| LP06 | At rest, type is solid, photo fill is zero, caption is fully opaque, and the correct image is sharp. |
| LP07 | Globe points to each supplied featured place; date-line wrapping, antipodal, and missing-coordinate cases are correct. |
| LP08 | Infinite navigation works in both directions, including negative and large positions, with bounded row counts. |
| LP09 | Wheel, touch, mouse, keyboard, dots, and arrows select correctly without unwanted focus or page-scroll capture. |
| LP10 | Look around supports pan/zoom/reset/return; hidden picker does not intercept focus or input. |
| LP11 | Tour starts paused; manual navigation pauses it; no idle visual loop or unnecessary rendering remains. |
| LP12 | Replay and scrub reuse the intended pair; browsing exits preview; preview events have no app side effects. |
| LP13 | Rapid selection and delayed loads cannot install stale photos or resources. No wrongly labeled stale scene. |
| LP14 | Reduced motion at startup and mid-transition clears all transient treatments; controls stay fully usable. |
| LP15 | High contrast, screen reader, focus, enlarged text, and all interactive labels are independently checked. |
| LP16 | Layouts at 320, 390, 768, and 1440 logical viewport widths, plus long captions and short windows, have no overlap. |
| LP17 | Missing photo, retry, unavailable shader, empty/single catalog, and device/context recreation are explicit and usable. |
| LP18 | All six final destination photos work offline with the chosen asset packaging and preserved source metadata. |
| LP19 | Two simultaneous cards remain independent; repeated navigation/unload cycles show bounded resources. |
| LP20 | Real-device startup, frame presentation, memory, and idle behavior are measured and reported, not assumed. |

A provisional engineering aim is smooth 60 Hz presentation on the selected reference devices, not an existing result. Report p50/p95/p99 frame intervals, missed presentation deadlines, cold/warm first-image time, memory before/after repeated loops, and idle work. Separate emulator/software-rendered results from hardware measurements. Keep per-target deviations explicit.

## 12. Deliverables and unresolved gates

Deliver the reusable Uno component, current destination data/assets, renderer decision record, app integration example, build/run commands, resolved versions and target heads, fixed-progress captures, motion recordings, accessibility and failure-state results, and a list of accepted visual differences. Supply relevant unit/integration/visual tests with the repository’s conventions.

Before release, resolve: exact shipping targets and Windows head; renderer capability and performance; fonts; full-photo acquisition and rights; final timing/intensity tuning. Do not silently substitute assets, switch renderers, or remove the photographic type treatment to claim completion. Report a precise blocker and complete unaffected work.

**Verification boundary of this brief:** source and implementation notes were reviewed; no Uno app was built or benchmarked for this update. The supplied browser source reports 105 prior Chromium fixture-based checks. That is historical web evidence, not a new run, an Uno test pass, full-photo availability proof, or a formal accessibility audit. [R3]

## Reference map

**R1 — Current visual authority:** `reference/destination-rolodex-living-postcard.html`. Use live interaction and final CSS overrides rather than earlier concept imagery.

**R2 — Design/behavior notes:** within the supplied source ZIP, `docs/LIVING-POSTCARD.md`, `docs/COHESIVE-ARRIVAL.md`, and `docs/GEOGRAPHIC-GLOBE-v4.md`.

**R3 — Implementation and evidence:** within that ZIP, `rolodex.js` (`_renderRows`, `_syncScrollScene`, `_arrivalFrame`, `_materialFrame`, `_tick`); `transition-engine.js`; `globe.js`; `destinations.json`; `tests/QA-RESULTS.md`. The new brief overrides older notes where their defaults conflict with Living Postcard.

Official technical references checked 8 October 2026. Apply them to the project’s installed versions; rolling documentation is not an instruction to upgrade.

- **S1:** Uno, *SKCanvasElement*. https://platform.uno/docs/articles/controls/SKCanvasElement.html
- **S2:** Skia, *SkSL & Runtime Effects*. https://skia.org/docs/user/sksl/
- **S3:** Uno, *Embedding Native Elements in Skia Apps*. https://platform.uno/docs/articles/features/using-skia-hosting-native-controls.html
- **S4:** Uno, *WebView2 (WebView)*. https://platform.uno/docs/articles/controls/WebView.html
- **S5:** Microsoft, *Performance best practices for WebView2 apps*. https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/performance
- **S6:** Natural Earth, *Terms of Use*. https://www.naturalearthdata.com/about/terms-of-use/
- **S7:** W3C, *Understanding SC 1.4.3: Contrast (Minimum)*. https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html
- **S8:** W3C, *Understanding SC 1.4.11: Non-text Contrast*. https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html
