# 05 · Motion and shader specification

## The approved effects

| Property | Contract |
| --- | --- |
| Production enum | MorphBlend or LiquidWarp only |
| Default | MorphBlend |
| Duration | 1200 ms default; API clamp 350–3000; comparison UI range 500–2400 |
| Intensity | 0.60 default; clamp 0–1 |
| Direction | +1 for forward, -1 for backward |
| Progress | 0 exactly equals source; 1 exactly equals target |
| Output | Opaque photo layer, clipped by the scene; not warped text or controls |
| Time source | Elapsed active time; no frame-count-based duration |

The 1200 ms default is deliberately observable in the comparison prototype. Do not shorten it or add overshoot without a review. Compare the same source/target pair at identical settings.

## Morph Blend

Both photographs are sampled through a shared low-frequency procedural flow. Noise combines three octaves with weights .58/.28/.14. Source and target luminance gently influence the displacement. A sine envelope removes distortion at the endpoints. The photos advect in opposite directions and blend through a spatially varying mask.

The authoritative function is `morph()` in the supplied GLSL. Preserve its field scale, offsets, luminance contribution, drift and mask. It is not a blur dissolve, opacity-only fade, or object-to-object morph. No learned model, landmark correspondences or generated in-between photo assets are needed.

Note that intensity zero does **not** eliminate all motion in the source algorithm: base deformation remains. Keep parity for this version. A separate reduction/fallback policy disables the effect when needed. Do not overload intensity zero to mean reduced motion without recording a spec change.

## Liquid Warp

An expanding circular reveal has an irregular edge. A localized refractive ring displaces sampling coordinates; the target image receives a subtle lens scale near that ring. Small color-channel offsets create an edge fringe, and a small edge highlight reinforces the refractive surface.

The authoritative function is `liquid()`. Preserve the direction-dependent origin, radius progression, edge width, displacement and small fringe. Do not replace it with a uniform ripple across the entire card or a glass overlay unrelated to the changing photograph.

The shader's origin is `(0.48, 0.38)` forward and `(0.48, 0.62)` backward **in bottom-left-oriented GLSL UV space**. With top-left UI coordinates those become `(0.48, 0.62)` and `(0.48, 0.38)`. Do not invert the origin twice. The SkSL candidate retains GL-oriented internal math and converts only at shader entry and image sampling.

## Spatial mapping and endpoint continuity

The scene is not the same size as its overscanned media layer. Both old/new photo sampling and shader output use the layer's actual local width/height. If the scene is 480×530, the nominal overscanned media group is 604.8×667.8 before camera transforms.

Cover sampling uses the image aspect and layer aspect, then offsets the unused crop by the focal point. Focal points in the manifest are normalized top-left values. Letterbox/pillarbox output is not acceptable. Ensure static image rendering and shader endpoints use the same transform, crop, color space and sampling. No visual jump is allowed when the shader hands back to the static image.

Clip the result to the original scene corners; keep the white detail section stationary. No black edges at maximum pan/zoom or during ripples. Repeat coverage checks after resize, high DPI and small landscape window changes. The current liquid reveal is tuned for this portrait card, not an arbitrary panoramic surface.

## Timing and text

During normal browsing the row spring runs independently; on cached selections, transition setup follows the logical country change. Details animate from opacity .22 / Y +4 to opacity 1 / Y 0 over 320 ms. Do not run the same opacity animation repeatedly for every progress update.

Debug replay uses `t*t*(3-2*t)` to move between source and target row positions while showing the image effect. Scrubbing is a deterministic test tool. It pauses the tour, uses the same last pair and supports 0, .25, .5, .75 and 1. Resuming navigation exits preview and restores a coherent selected image.

The country glyphs remain clear and unwarped. No liquid letter morph, text smear or bitmap text texture is approved by the live prototype.

## Rendering gate

Compile the SkSL candidate on a supported target, compare each mode's endpoints and fixed-progress frames with the GLSL reference, then inspect actual playback. GL noise and Skia color management may introduce small differences; geometry, order, origin, crop and dominant effect must match. Use raw shader frames without the text overlay for effect comparison, then compare the full card.

Acceptance is not a static screenshot alone. Require a recording of both directions, a rapid-input test, a reduced-motion test and a fallback test. Measured frame-time targets are defined in the acceptance brief; none have been claimed in this handoff.

See `uno-reference/shaders/UNIFORMS.md` and S2/S3 in the Sources brief.
