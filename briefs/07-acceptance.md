# 07 · Acceptance and verification

These are rebuild acceptance criteria, not claims that a Uno app already passed. See `qa/browser-results.json` for checks performed on the HTML handoff. Those checks use explicit fixtures.

| ID | Test | Pass condition |
| --- | --- | --- |
| A01 | Two approved modes | Only MorphBlend and LiquidWarp are advertised; no Orbit/Slice mode |
| A02 | Live implementation | Input changes real destination state, photograph and copy; no static-image substitute |
| A03 | Desktop/mobile layout | Card hierarchy and proportions match; 390×844 / 1440×1040 have no horizontal overflow |
| A04 | Taxonomy/copy | Six countries and original descriptions; no city-as-country labels or reference logo |
| A05 | Bidirectional wrap | Previous from Philippines reaches New Zealand; next returns to Philippines |
| A06 | Picker pool | Bounded row count; no collection growth after 1,000 steps |
| A07 | All inputs | Wheel, swipe, pointer, keyboard, indicators and button navigation operate coherently |
| A08 | Focus/type-ahead | Home/End, Page keys and letter search work only in the appropriate context |
| A09 | Effect endpoints | Progress 0/1 matches static photo with the same crop/focal/camera |
| A10 | Morph evidence | Shared displacement visible at intermediate frames; not an opacity-only dissolve |
| A11 | Liquid evidence | Direction-dependent moving lens/reveal; no full-screen unrelated ripple |
| A12 | Stable UI | Text and controls remain sharp; detail panel and card border do not warp |
| A13 | Geometry/DPI | No black edge, stretching or origin flip at resize, high DPI or camera extremes |
| A14 | Timing | Measured duration matches setting within two displayed frames on the tested device |
| A15 | Rapid retarget | Latest request wins; no stale completion or unbounded queue; selected/photo state coherent |
| A16 | Loading/failure | Own preview or explicit neutral status, working Retry, no unlabeled old photograph |
| A17 | Look around | Same destination; pan/zoom bounded; reset works; Escape returns focus |
| A18 | Tour | Play/pause and 5500 ms demo cadence; manual selection pauses; reduced motion respected |
| A19 | Background/unload | No unwanted advances while hidden, no orphan callbacks/resources after unload |
| A20 | Reduced motion | No procedural deformation or passive parallax/drift; content remains browsable |
| A21 | No shader support | Usable, explicitly reported fallback; no crash during surface construction |
| A22 | Data edges | Empty/one-item collection, data replacement, duplicate IDs, missing assets handled |
| A23 | Accessibility | Real XAML semantics, non-duplicated selection, keyboard access, large text and contrast |
| A24 | Offline assets | Six validated packaged originals; no network required for in-app photo display |
| A25 | Multiple instances | Settings, progress, camera and selection do not leak between cards |
| A26 | Resources/performance | Bounded cache, no per-frame decode/compile, no growing GPU/managed memory trend |
| A27 | Platform evidence | Exact OS/device/renderer/SDK builds named; no untested-target success claims |
| A28 | Final parity proof | Fixed-progress captures plus playback recording for both modes, both directions |

## Performance targets, not measurements

For the agreed baseline device and a single visible card, aim for a 60 Hz presentation budget: approximately 16.7 ms per frame, with warmed p95 frame time at or below that budget. Record lower-end performance instead of hiding misses. Gesture feedback should begin within 100 ms when images are cached. Decode/upload belongs on the cold path, not each frame. Set a practical cache budget and measure a 1,000-step loop; after cleanup, memory should plateau rather than grow per navigation.

The HTML caps uploaded textures at 1600 pixels on the longest side and its render buffer to roughly 1.5 million pixels, with DPR capped at 1.6. These are reference implementation guards, not a prescription to permanently blur a native high-DPI control. Profile and select a native budget that preserves the intended appearance.

## Visual methodology

Use the same two source photographs, dimensions, focal positions, intensity .60 and 1200 ms duration for comparisons. Capture raw media at progress 0/.25/.5/.75/1 in both directions. Capture the full card at rest, mid-transition, Look around, loading, fallback and reduced motion. Compare geometry/crop separately from color management and font rendering. The synthetic fixture test proves the rendering path, not photographic fidelity.

## Evidence record

For each run record date, commit, target framework, device/OS, renderer backend, .NET/Uno/SkiaSharp versions, display scale, media IDs/hashes, effect settings, checks passed/failed, measured timings and resource trace location. Test .NET Release configurations as well as Debug before calling it production-ready. Add screenshots or recordings only when they show actual executed output.
