# Shader binding and port notes

The GLSL is extracted from the executable approved prototype. The SkSL is a candidate, not compiled proof. Compile it with the installed SkiaSharp runtime-effect API and record diagnostics before relying on it.

| Name | Type | Meaning |
| --- | --- | --- |
| uFrom / uTo | shader children | Source and target SKImage shaders, clamp tiling, linear filtering, identity local matrix |
| uProgress | float | Normalized elapsed active time, 0..1 |
| uStrength | float | Intensity, 0..1; default .60 |
| uDirection | float | +1 or -1 |
| uMode | int | 0 Morph Blend; 1 Liquid Warp |
| uResolution | float2 | Actual overscanned layer width/height in the same local units as main(coord) |
| uAspect | float | uResolution.x / uResolution.y |
| uAspectA/B | float | Decoded source/target width divided by height |
| uImageSizeA/B | float2 | Decoded source/target dimensions in image pixels |
| uFocalA/B | float2 | Normalized top-left object-position from manifest |

Do not supply scene dimensions if drawing the overscanned media plane. Do not add device scale to local shader coordinates twice. Respect RenderOverride's local origin and clipping area. If your host canvas is translated/scaled for the media plane, pass the matching local dimensions.

SkSL main() receives local top-left coordinates; the port converts to bottom-left UV, performs the original effect and cover math, then converts sample coordinates back to top-left image pixels. Because child shaders sample pixels, multiplying by uImageSize is intentional. Avoid an extra image local matrix that silently duplicates the cover transform.

The six selected inputs are opaque JPEG photos. The original shader returns alpha 1. The candidate preserves that output contract; transparent media requires a separately validated premultiplied-alpha implementation. Skia color management can affect luminance and blends compared with a browser GL reference; compare consistent color-space inputs and outputs.

Maintain exact endpoint shortcuts before either effect. Static photos must use the same cover/focal geometry as the shader, or holding a timeline endpoint and ending playback will visibly jump. Keep uniforms stable for a frame. A source/target resource lease must outlive the complete draw and any queued transition.

Compile effects once, not in each RenderOverride. Decode images before entering the draw path. Cache paints/effects/children under explicit ownership; follow the installed API's disposal requirements. The wrapper should not perform network IO, JSON parsing or broad UI binding updates from the renderer.

Official details: S1/S2/S3 in `briefs/09-sources.md`.
