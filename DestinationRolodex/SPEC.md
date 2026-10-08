# Destination Rolodex · Uno Platform build spec

The handoff briefs (`../briefs/01` to `09`) are the product, design, interaction, motion and acceptance spec. This file
records the Stage 0 decisions those briefs left to the build machine, the capability inventory, the quality gates, and the
graphs the app actually runs as. When this file and a brief disagree, the brief wins unless the row below says why not.

## Architecture Brief

| Decision | Value | Reason / tradeoff |
| --- | --- | --- |
| Template | `dotnet new unoapp -preset blank -tfm net10.0 -presentation mvvm -markup xaml -theme simple -platforms desktop android wasm -renderer skia -toolkit -tests unit` | Brief 04 asks for XAML + MVVM; blank keeps Navigation/DI/HTTP out (brief 04: no service dependencies). Tradeoff: no region navigation; the demo is one page, so none is needed. |
| Resolved stack | .NET SDK 10.0.303 · Uno.Sdk 6.7.30 (Uno.WinUI 6.7.135, Graphics2DSK 6.7.135, Toolkit 9.1.3, Simple 7.1.1) · SkiaSharp 3.119.2 (bundled) · CommunityToolkit.Mvvm 8.4.2 · Uno.Templates 6.7.30 | Pinned in `global.json` and `Directory.Packages.props`. SkiaSharp 3.119 already has `SKRuntimeEffect`/`SKRuntimeShaderBuilder`; no v4 opt-in needed. |
| Heads | `net10.0-desktop` (Skia, Win32 host) primary; `net10.0-android` (Skia renderer); `net10.0-browserwasm` (Skia). WinAppSDK and iOS/macOS not scaffolded. | Brief 01 validation order. WinAppSDK is a separate renderer architecture (brief 04); iOS needs a Mac host. Both recorded as untested in the evidence record. |
| Presentation | MVVM (CommunityToolkit) for the demo page; the reusable card is a `UserControl` with dependency properties and internal controllers (picker, camera, transition coordinator, media cache). Card XAML binds with `x:Bind` to a typed internal state object. | House rule: MVVM for samples/demos. The card keeps per-frame animation out of INPC (brief 04: no per-frame PropertyChanged). |
| Photo surface | `SKCanvasElement` subclass (`PhotoTransitionSurface`) created lazily after `IsSupportedOnCurrentPlatform()`; one `PhotoRenderer` per card owns a compiled `SKRuntimeEffect` + builder + paints; frames are immutable `PhotoFrame` records. Static frames render through the same shader at progress 0, so endpoints equal the static image by construction. | Brief 04/05. Fallbacks: runtime effect missing → plain cover image + 400 ms crossfade (`PlainImage`); no Skia surface → XAML `Image` (`Unavailable`). Both reported through `RendererStatus`, never labelled as an effect. |
| Camera | CSS `perspective(1000) translate rotateX rotateY scale` folded into one 3x3 projective `SKMatrix` applied to the overscanned layer inside the renderer. | Keeps XAML text untransformed and the shader/static mapping identical. |
| Media | `DestinationMediaCache`: thread-pool decode, 1600 px longest-side guard, 96 MB LRU budget with pinned ids, in-flight de-dup, retire queue disposed two frames + 500 ms after retirement. | Brief 04 resource rules. No preview crop is shipped (D10: rights not established); loading shows the destination colour plus an explicit status pill. |
| Retargeting policy | Bounded latest-target: a running transition finishes (≤ duration), then the newest ready target plays. | D17. Tradeoff: photo can trail rapid input by up to one duration; selection text never does. Snapshot retargeting deferred. |
| Reduced motion | `IsReducedMotionEnabled` dependency property (host setting + demo toggle + `--reduced`). | `UISettings.AnimationsEnabled` is hard-coded true on Skia desktop (spec-time fact), so the OS cannot drive it there. |
| Font | Inter 4.1 static instances (OFL 1.1) `Inter_Regular/Medium/SemiBold/Bold.ttf` in `Assets/Fonts`, underscore names (Android rewrites hyphens). Country rows use SemiBold (closest static to CSS weight 650). | D19. Tracking −0.057 em: `CharacterSpacing` is a no-op on Uno Skia, so rows are laid out one glyph per `TextBlock` with a negative margin. Delta: pair kerning is lost at display size. |
| Debug harness | DEBUG-only command-line switches (`--export-fixtures`, `--capture`, `--viewport`, `--mode`, `--seek`, `--look`, `--reduced`, `--no-shader`, `--media-fail`, `--media-delay`, `--steps`, `--perf`, `--spike`). | The App MCP is not available in the authoring session; captures and the 1,000-step loop run headless through these switches. |

### Capability inventory

| Capability the design implies | Status | Choice |
| --- | --- | --- |
| Procedural photo morph (Morph Blend) | implemented | SkSL port of `morph()` on `SKCanvasElement` |
| Refractive reveal (Liquid Warp) | implemented | SkSL port of `liquid()` |
| Infinite looping picker, nine recycled rows | implemented | `CountryPicker` control, `CircularMath` |
| Pointer parallax + Look around pan/zoom/tilt | implemented (API only) | `CameraController`, matrix in renderer; the in-card Look around button was removed 2026-10-08 at the user's request, `ViewMode = Look` stays reachable from code and the `--look` harness switch |
| Tour autoplay with progress ring | implemented | frame loop + dashed `Ellipse` stroke |
| Loading / failure / retry | implemented | status pill + Retry; `--media-fail`/`--media-delay` prove it |
| Reduced motion | implemented | DP + demo toggle; no OS hook on Skia desktop |
| Photo credits dialog | implemented | `ContentDialog` listing credit + source link per destination |
| Blur on distant picker rows (≤ 1 px) | omitted | XAML has no cheap per-element blur on Skia; brief 03 allows the delta |
| Text shadow under country rows | omitted | no XAML text shadow; the stable wash carries contrast |
| Picker top/bottom fade mask | substituted | folded into per-row opacity near the 300 px window edge |
| Row `rotateX`/`translateZ` perspective | substituted | folded into scale (≤ 1.6 % trapezoid at 12°, imperceptible); no `PlaneProjection` dependency |
| Video media type | omitted | brief 01: video out of scope for this rebuild |
| Loading preview crop | omitted | D10 rights; neutral colour surface instead |

## Design Brief

Tokens live in `Styles/DestinationRolodexResources.xaml` (theme-invariant: the card is a light design painted over photographs,
so ink belongs to the surface, not the app theme). Colors from `design/tokens.json`; CSS `RRGGBBAA` converted to XAML `AARRGGBB`.
Type: Inter static instances; sizes from brief 02 (country `clamp(36, w×.1245, 64)`, long names `clamp(34, w×.113, 59)`,
location 12/600, region 9/500 +1.45 px, description 13/1.65). Icons: the 15 supplied 24-unit stroke paths as keyed geometry
in `Styles/Icons.xaml`, drawn with stroked `Path` at 1.45 units. Card: max width 480, radius 26 (23 narrow), shadow stack via
`utu:ShadowContainer`. Scene height: `clamp(365, .58×viewport height, 530)`; narrow `clamp(340, .55, 510)`; 360 when height ≤ 760 and width > 560.

## Interaction Brief

Brief 03 verbatim. Input lands on the scene grid (wheel, drag with capture, click-to-row), on the picker (arrow/page/home/end/
type-ahead) and on the scene in Look mode (arrows pan, +/- zoom, Escape back). Wheel units: Uno reports ±120 per notch; the
card maps it to the CSS 100-per-notch scale, then `/115` with the ±180 clamp. States: Browse/Look, Playing/Paused,
Empty/Loading/Ready/Failed, Shader/PlainImage/Unavailable, Full/Reduced motion, Off/Replaying/Scrubbing (demo only).

## Spec Graph Brief

```text
MainPage (demo)                                  DestinationDemoViewModel (MVVM)
├─ Header: brand · LIVE TRANSITIONS
├─ Left column
│  ├─ "Where to next?" + "6 destinations"
│  ├─ DestinationRolodexCard #1   ← Destinations, TransitionKind, Duration, Strength, IsReducedMotion (two-way to VM)
│  │  ├─ Scene (clipped, Height=SceneHeight)
│  │  │  ├─ PhotoTransitionSurface | fallback Image     (lazy; RendererStatus)
│  │  │  ├─ Wash gradients · Readability radial
│  │  │  ├─ TopBar: globe DESTINATIONS · 01/06 · Play ring button
│  │  │  ├─ Motion badge · Media status pill (+Retry)
│  │  │  ├─ CountryPicker (9 rows, markers, focus ring, LiveSetting status)
│  │  │  ├─ Look title · Photo credit link
│  │  │  └─ Footer: hint | camera controls (Look mode via ViewMode only; no button)
│  │  └─ Detail: pin location · REGION · description · dots(6) · prev/next
│  ├─ Under card: instructions · "About the photos" → ContentDialog !
│  └─ DestinationRolodexCard #2 (toggle; LiquidWarp, independent)
└─ Motion lab (right / stacked ≤ 720)
   ├─ Effect radio ×2 ⇒ VM.TransitionKind → card
   ├─ Duration 500–2400 · Intensity 0–100 ⇒ VM → card
   ├─ Timeline 0–1000 ⇒ card.SeekTransitionAsync (debug)
   ├─ Route from → to · Replay (R) ⇒ card.ReplayTransitionAsync
   ├─ Status line ← card.RendererStatus / events
   └─ Demo toggles: Reduce motion · Second card
```

Spec gate: no routes (single page, no Navigation extension); every bound node has a DP or VM property; FeedView n/a (no MVUX);
every color resolves to a named token in the card dictionary (theme-invariant by design, named `*Fixed`); motion durations are
constants in `DestinationRolodexCard` (320 ms detail, 360/600 ms look, 1200 ms default transition) with a reduced-motion branch.

## Implementation Plan

Stages 0–7 of `briefs/06-implementation-plan.md`, each committed. Evidence under `docs/evidence/`.

## Quality Gates

| Phase | Gate (skill) | Done when |
| --- | --- | --- |
| Every stage | `dotnet build -f net10.0-desktop` 0 warnings; `dotnet test` green | build log in the commit |
| Stage 1 | fixture export (`--export-fixtures`) | endpoints byte-equal to static, both modes, both directions, camera coverage 0 exposed px |
| Stage 2–5 | `uno-verify` equivalent through harness captures (App MCP unavailable in this session) | captures at 390×844 and 1440×1040, look, loading, failed, reduced, no-shader |
| Stage 4 | `uno-component-states` | Loading/Empty/Error exist; Retry proven with `--media-fail` |
| Stage 6 | `uno-completeness-audit` scoped to the card; A01–A28 matrix in `docs/evidence/ACCEPTANCE.md` | each row has evidence or is marked untested |
| Stage 7 | `handoff` → `HANDOFF.md` | current |

## Risks

- API: `SKRuntimeEffect.CreateShader` on SkiaSharp 3.119.2 with this SkSL — spike (Stage 1 export) — resolved by the report.
- API: perspective CTM with a runtime shader on the GL window canvas — verified visually on the spike page.
- Platform: Android `StorageFile` for `ms-appx` JPEGs under `Assets/Destinations` — check on the emulator; fallback is reading
  `Android.Content.Res.AssetManager` directly.
- Platform: WASM runtime-effect support on the browser Skia backend — run and record.

## Unresolved Questions

- Blur on distant rows, text shadow and the fade mask are substituted/omitted as listed above; accept or ask for a Skia-text layer (rejected by brief 03).
- WinAppSDK head and iOS/macOS: not scaffolded; untested by design.
- Final duration/intensity remain the inherited 1200 ms / 0.60 (D06).
- Norway and Iceland source pages are still unverified (manifest `sourcePageVerified: false`).
- App MCP verification in a rooted session remains a release gate; this session verified through DEBUG harness captures.

---

# Revision 2 · Living Postcard (2026-10-08)

Source: `../briefs/destination-rolodex-uno-living-postcard-brief.md` and its kickoff. The brief's visual authority
(`reference/destination-rolodex-living-postcard.html` and the source zip with `rolodex.js`, `globe.js`, the coastline
bitmask and `docs/`) is **not present** in this kit; the implementation follows the brief's measurements, formulas and
tables, and every place where only the source could settle a detail is listed below as an assumption.

## Decisions

| Decision | Value | Reason / tradeoff |
| --- | --- | --- |
| Renderer | Keep the native path: XAML text/controls over `SKCanvasElement` (photo effects, camera), a second translucent `SKCanvasElement` above the rows for photo-in-type, and a small `GlobeElement`. WebView2 not adopted. | The native spike already proved both effects, clipping and the surface; a WebView would re-host the whole card and lose native semantics. Recorded as the renderer decision; the brief allowed either. |
| Shared state | `SceneFrame.Sample(q, N)` (low, p, from/to, selected, travel = sin πp) drives rows, shader uniforms (ordered pair low→low+1, fixed direction +1, progress p), globe slerp, photo-in-type opacity, caption treatment, material colors and arrival camera. Browsing no longer starts a timed transition on selection change. | Brief §3. The timed `TransitionCoordinator` stays only for the lab's replay/seek (preview flag) and the settled media identity. |
| Spring | 115 / 21, substeps 1/120 s, catch-up bounded at 0.5 s, monotonic clock. | Brief §4 motion table. |
| Composition | One photo behind everything, warm rim 6 (`#EBE6DC`), outer radius 30 (27 narrow), inner radius = outer − rim, feathered scrim strongest behind the caption, inner highlight, destination-tinted shadow; no counter, no in-photo badge; tour starts paused; no drift. | Brief §2 / change table. |
| Type | Country: Inter SemiBold `clamp(34, .1235 w, 61)`, tracking −0.055 em (per-glyph). Place: Source Serif 4 Regular 23 / 20 compact (OFL, bundled; Georgia is not on every head). Description Inter 12 / 1.7, `#EEEADE`. | Brief §2; Georgia substitution recorded. |
| Globe | Orthographic, Natural Earth 110m land mask regenerated (`Assets/Globe/landmask.bin`, 1-bit 512×256), 96 px buffer shown at 27/25, slerp on unit vectors with deterministic antipodal path, neutral unpinned globe when coordinates are missing. | Brief §5; the bundled bitmask was unavailable, provenance in `Assets/Globe/NATURAL_EARTH.md`. |
| Material | pigment = lerp(color_from, color_to, p); ink = .85 neutral(24,25,23) + .15 pigment; accent = .80 warm(255,247,224) + .20 pigment; shadow tint = .35 pigment over neutral. Accent paints the selected dot, marker and globe pin; ink is unused on the dark scrim (kept for the shadow and future light surfaces). | Brief §4; where the HTML applies "caption ink" is an assumption. |
| Photo-in-type | Per visible row, its own photo cover-cropped to the scene, drawn through the row's glyphs (Skia text with the same Inter face, positioned from the XAML glyph bounds), alpha = travel × .60 × max(0, 1 − 0.72·|d|), above the solid XAML text. | Brief §4; mask cached per row/text/size. |
| Lab | Morph Blend / Liquid Warp, strength, replay duration, replay, scrub, and a diagnostic **Original Crossfade** (lab-only `DiagnosticCrossfade`, never part of `TransitionKind`). | Brief §8. |
| Polish pass (2026-10-08, ui-craft + impeccable:polish) | Page `#E6E8EC` slate (user decision); Look around button removed (Look mode stays API-only); failed selected photo shows the neutral backdrop, never a neighbor's photograph; pagination fits six targets at any width (30 px, 21 px below a 300 px card); hint text restored at 320; lab option copy wraps (Grid, not horizontal StackPanel); demo greys collapsed to two AA tokens on the slate page (Muted 5.5:1, Faint 4.5:1, sage hue kept); lab sliders use `SimpleSliderStyle` + app-level lightweight keys; "About the photos" is a bare button (single underline). | Brief §9 (no mislabeled photograph), WCAG AA for 8–12 px demo copy. Tradeoffs: 21 px dot targets below 300 px card width; demo chrome keeps the prototype's sage hue on a cool page (user chose slate; recolouring the chrome is a separate decision). |

## Assumptions (would be settled by the missing source)

- Scrim gradient stops, inner highlight strength and shadow blur radii are chosen to read like the brief's description.
- Caption ink placement; pointer-lighting highlight size; dot/button exact fills.
- `_syncScrollScene` behaviour when a pair photo is not yet decoded: the ready endpoint is shown static at the current fraction's nearest side, with the status pill after 1.1 s.
- Whether the demo header keeps "6 destinations": kept.

## Unresolved Questions (added)

- Provide `reference/destination-rolodex-living-postcard.html` and `destination-rolodex-living-postcard-source.zip` to replace the assumptions above and to run a browser parity pass against revision 05.
- Fonts: Source Serif 4 stands in for Georgia; confirm or supply the intended serif.
