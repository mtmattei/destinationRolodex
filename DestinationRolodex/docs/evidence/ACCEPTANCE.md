# Acceptance record · A01–A28

Run: 2026-10-07 · commit: see `git log` (branch master) · Windows 11 Pro 10.0.26200 · display scale 100% (captures at 1x)
· `net10.0-desktop` Debug, Skia renderer on the Win32 host (OpenGL) · .NET SDK 10.0.303 · Uno.Sdk 6.7.30 (Uno.WinUI 6.7.135)
· SkiaSharp 3.119.2 · media: the six manifest originals (`assets/manifest.json` hashes after `tools/fetch_assets.py`)
· effect settings: Morph Blend / Liquid Warp, 1200 ms, strength 0.60.

Evidence folders: `fixtures/` (raw photo-layer frames from the real renderer, CPU raster backend, same SkSL),
`browser-reference/` (the approved HTML's WebGL frames with the same photos, cropped to the scene, plus side-by-side
composites and `report.json` with mean |diff|), `captures/` (full-card captures through the DEBUG harness,
`RenderTargetBitmap` of the page root, with per-run logs and JSON statistics).

Desktop was executed through the harness; WebAssembly was executed in headless Chromium (`wasm/`, `tools/wasm_smoke.py`,
Debug build served by dotnet-serve): the runtime effect compiles and renders on the browser Skia backend and the tour
advances. Android builds (0 warnings) and its runtime is parked at the user's request. WinAppSDK and iOS/macOS were not
scaffolded (SPEC.md).

| ID | Test | Result | Evidence |
| --- | --- | --- | --- |
| A01 | Two approved modes | PASS | `PhotoTransitionKind` has `MorphBlend`, `LiquidWarp` only; `TransitionSettings` rejects others; lab shows two options (`captures/desktop-rest.png`). The shader's third branch (plain ease) is unreachable from the API. |
| A02 | Live implementation | PASS | Selection drives real state: `captures/desktop-liquid-050.png` (rows synced, counter 02/06, detail copy for Japan, photo mid-reveal); `--steps=1000` changes `selectedIndex` to 4 (`desktop-steps.png.json`). |
| A03 | Desktop/mobile layout | PASS (desktop 1440×1040, 390×844) | `captures/desktop-rest.png` vs `browser-reference/card-desktop.png`; `captures/mobile-rest.png` vs `browser-reference/layout-mobile-390x844.png`. No horizontal overflow (page `ScrollViewer` horizontal scrolling disabled; card `MaxWidth` 480). |
| A04 | Taxonomy/copy | PASS | Six countries from `Assets/Data/destinations.json` in the original order; descriptions verbatim; no logo. Counter and dots show 06. |
| A05 | Bidirectional wrap | PASS | Unit test `Previous_from_first_wraps_to_last_and_next_returns` (selection 5 then 0). |
| A06 | Picker pool | PASS | Nine fixed rows (`CountryPicker.RowPool = 9`); 1,000 steps: position normalized, `cacheCount` 6, `retired` 0, managed heap 31.9 MB after GC (`desktop-steps.png.json`); unit test `Thousand_steps_keep_position_bounded_and_land_correctly`. |
| A07 | All inputs | PARTIAL | Wheel/drag/click/keyboard/indicator/button paths are implemented in `DestinationRolodexCard` and covered by controller unit tests (wheel clamp + snap, drag momentum, cancelled drag, row click, type-ahead). Real pointer/keyboard injection was not run in this session (no App MCP); listed as a remaining gate. |
| A08 | Focus/type-ahead | PARTIAL | Home/End/Page keys and letter search only handled when `OriginalSource` is the picker or the scene (`OnKeyDown`); type-ahead unit test passes. Live keyboard run pending (see A07). |
| A09 | Effect endpoints | PASS | `fixtures/report.json` `endpointChecks`: `maxDiffAt0 = 0` and `maxDiffAt1 = 0` for both modes, all three pairs (byte-equal to the static frame; same shader, same crop/focal/camera). |
| A10 | Morph evidence | PASS | `fixtures/fixed/morph-ph-jp-050.png` and the live on-screen frame `captures/desktop-autoplay-mid.png` (autoplay morph at 32 %, GPU path): shared displacement visible; `midpointVsCrossfadeMeanDiff` 14.9–18.9 (not an opacity dissolve); browser parity mean |diff| 1.1–2.9 (`browser-reference/report.json`, `compare-morph-*.png`). |
| A11 | Liquid evidence | PASS | `fixtures/fixed/liquid-ph-jp-050.png` (forward, origin lower) vs `liquid-jp-ph-050.png` (backward, origin upper): direction-dependent lens reveal; browser parity mean |diff| 0.5–2.0. |
| A12 | Stable UI | PASS | Text and controls are XAML (`CountryPicker` rows are `TextBlock`s; the surface is below them). `captures/desktop-liquid-050.png`: labels, detail panel and card border unwarped mid-effect. |
| A13 | Geometry/DPI | PASS (1x) / UNTESTED (high DPI) | `fixtures/report.json` `cameraCoverage`: 0 exposed pixels at ±37 px pan, ±2.8°/±3.8° tilt, zoom 1.65 and at the browse amplitude. Camera matrix unit tests. Scene clip follows the measured card width. 150%/200% display scale not run. |
| A14 | Timing | PASS (loop measured) | Progress is elapsed active time (`TransitionCoordinator.Tick`); frame loop p50 15.54 ms, p95 17.27 ms, max 47.7 ms over 12 s (`captures/perf.json`), so a 1200 ms effect lands within one frame of 1200 ms. Unit test `Endpoints_and_progress_follow_elapsed_time`. |
| A15 | Rapid retarget | PASS | Unit test `Latest_target_wins_without_an_unbounded_queue`: a→b runs to completion, only the newest target (c) follows, a request for the photo already in flight empties the queue. 1,000 rapid steps settle on index 4 with 6 cached photos. |
| A16 | Loading/failure | PASS | `captures/desktop-loading.png` (`--media-delay=30000`): own colour surface + "Loading Italy photo…" pill; `captures/desktop-failed.png` (`--media-fail=jp`): "Photo unavailable · check connection" + Retry; `Retry()` invalidates the cache entry and reloads. No stale photo under a new country after the 250 ms grace. |
| A17 | Look around | PASS | `captures/desktop-look.png`, `captures/mobile-look.png`: same destination, picker hidden, title + credit + camera pill; pan/zoom clamped (`CameraController` tests); Escape returns focus to the picker (`OnKeyDown`). |
| A18 | Tour | PASS | `captures/desktop-autoplay-mid.png` (ring arc after 6 s of autoplay); interval 5500 ms with a 2500 ms floor; manual selection calls `Pause()`; reduced motion blocks the default tour (`InitializeData`). |
| A19 | Background/unload | PASS (static) | `EffectiveViewportChanged` and `XamlRoot.IsHostVisible` gate the loop; `Unloaded` stops the timer, cancels loads, releases the surface and disposes the cache. Not exercised with a hidden window in this session. |
| A20 | Reduced motion | PASS | `captures/desktop-reduced.png`: immediate selection; `IsReducedMotionEnabled` disables the effect (`ShowImmediately`), parallax, drift, tilt and default autoplay (`CameraController` test `Reduced_motion_removes_tilt_and_browse_parallax`). |
| A21 | No shader support | PASS | `captures/desktop-noshader.png` (`--no-shader`): badge "PLAIN PHOTO · NO SHADER", status "Plain photo crossfade · runtime effect unavailable"; scrub falls back to a plain selection with a fallback message. `IsSupportedOnCurrentPlatform()` is checked before construction; the XAML `Image` path handles the no-surface case. |
| A22 | Data edges | PASS | `captures/desktop-empty.png` (empty state, nav disabled), `captures/desktop-single.png` (one item: duplicates suppressed, prev/next/play disabled); duplicate ids throw in `DestinationRepository.Parse` and `InitializeData`; missing asset → A16 path. |
| A23 | Accessibility | PARTIAL | Real XAML semantics: picker is the single focusable selection surface (`AutomationProperties.Name`), rows are `AccessibilityView=Raw`, dots/nav/play/look/zoom/retry have names, settled manual changes update a `LiveSetting=Polite` announcer, hit regions ≥ 44 px (dots 28×44). Screen-reader, large-text and contrast passes were not run. |
| A24 | Offline assets | PASS | Six originals packaged under `Assets/Destinations` (ids ph, jp, it, no, is, nz; 1400×1050 / 1400×933 / 1280×853); loaded through `ms-appx`; no network path exists in the app. |
| A25 | Multiple instances | PASS | `captures/desktop-second.png`: card 2 (Liquid Warp, index 3, paused) beside card 1 (Morph Blend, index 0, playing); every controller, cache, surface and renderer is per instance (no statics except the SkSL source). |
| A26 | Resources/performance | PASS (desktop) | Effect compiled once per card; one short-lived `SKShader` per frame; decode on the thread pool; cache budget 96 MB with pinning and deferred disposal; 1,000 steps: 6 entries, 31.8 MB, working set 450 MB; `RenderOverride` p95 0.056 ms (command recording; GPU work is asynchronous); loop p95 17.3 ms. Release configuration not profiled. |
| A27 | Platform evidence | PASS for desktop and WebAssembly (1440×1040); others labelled | Desktop: Windows 11 Pro 10.0.26200, Win32 host, Skia/OpenGL, Debug. WebAssembly: Chromium headless (SwiftShader), Debug, `wasm/wasm-desktop-1440x1040.png`, no page errors (console shows only devserver websocket refusals). Android: builds, runtime parked. WinAppSDK, iOS, macOS: not built. |
| A28 | Final parity proof | PASS | Fixed-progress captures for both modes and both directions (`fixtures/fixed/*`), browser side-by-sides (`browser-reference/compare-*.png`), playback frame sequences (`fixtures/frames/*`, 37 frames each) assembled into `recordings/*.mp4`. |

## Performance

| Metric | Value | Source |
| --- | --- | --- |
| Frame-loop interval, 12 s autoplay with effects | p50 15.54 ms · p95 17.27 ms · max 47.69 ms | `captures/perf.json` |
| `RenderOverride` duration (command recording) | p50 0.032 ms · p95 0.056 ms | `captures/perf.json` |
| CPU-raster full shader frame (fixture export, 480×530) | p50 95.7 ms · p95 151.6 ms | `fixtures/report.json` (offscreen reference path, not the on-screen path) |
| 1,000-step loop | 6.0 s · cache 6 entries / 31.8 MB · 0 retired · managed 31.2 MB | `captures/desktop-steps.png.json` |

Interval spikes above 100 ms appear only in the 1,000-step run (first decodes under a synchronous step burst); the 12 s autoplay run's worst frame was 47.7 ms. Release builds were not measured.

## Known deltas (also in SPEC.md)

- Distant-row blur (≤ 1 px) and the country text shadow are omitted; the fade mask is folded into row opacity; the row perspective is folded into scale.
- Per-glyph tracking loses pair kerning in country labels.
- Hit regions: dots are 28×44 (row width), everything else ≥ 44×44.
- Loading preview crop not shipped (rights); a colour surface with a status pill is used instead.
