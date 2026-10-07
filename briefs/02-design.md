# 02 · Design brief

## Preserve the actual component

The approved prototype is light, quiet and compact: an off-white page, muted green interface, white detail panel and photographic scene. The card is not the dark, glowing glass card in the generated concepts. Do not add travel logos, flag icons, ornamental orbit lines, extra buttons or marketing copy.

The image is the visual emphasis. Keep country labels and controls separate from the photo shader. The active country is opaque white, adjacent entries are translucent, and entries farther away become faint and leave the visible window. The photo changes behind the stable card frame; neither the whole card nor its description bends in the shader.

## Layer order

```text
Card border, rounded clipping and shadow
  Scene viewport (clipped)
    Overscanned media group, camera-transformable
      Old/new photographs or the shader output
    Readability gradients (stable)
    Top bar: globe / DESTINATIONS, index, play/pause
    Country picker: recycled rows, active markers
    Look-around title (only in photo exploration)
    Bottom overlay: input hint, Look around / Back to list
    Loading/retry status (when needed)
  White detail area
    Location + region
    Short description
    Position indicators + previous/next controls
```

The DOM prototype uses a shader canvas above two photo layers, all inside the same overscanned media container. In Uno, the rendering implementation may use a single runtime-effect surface, but its output must occupy the same visual layer and share the same camera mapping.

## Dimensions and responsive rules

Values below are observed CSS values. Treat them as logical-pixel starting points in XAML, not physical device pixels. `design/tokens.json` is the machine-readable companion.

| Element | Reference value |
| --- | --- |
| Card maximum width | 480 |
| Card radius | 26; mobile host override 23 |
| Scene height | clamp(365, 0.58 × small viewport height, 530) |
| Mobile scene | clamp(340, 0.55 × small viewport height, 510) |
| Compact desktop scene | 360 when viewport height ≤760 and width >560 |
| Hard scene minimum | 340 |
| Overscan | 13% on each side, creating a 1.26× media layer |
| Country picker center | 49.5% of scene height |
| Picker mask window | 300 high; 266 when card width ≤360 |
| Logical row pitch | clamp(card width × .17, 58, 80) |
| Country font | clamp(36, card width × .1245, 64), weight 650 |
| Long country font | clamp(34, card width × .113, 59) for >11 characters |
| Country tracking | -.057 em; verify actual typeface metrics |
| Detail padding | top 23, right 25, bottom 15, left 25 |
| Detail small-width padding | 20, 20, 13, 20 |
| Location / description | 12 semibold / 13 regular, description line-height 1.65 |
| Topbar inset | top 20, left 26, right 20 |
| Scene footer inset | bottom 21, left 24, right 21 |
| Play visual circle | 38 × 38 |
| Previous/next visual bounds | 40 × 36 |

For the Uno port, expand interactive hit regions to at least 44×44 logical pixels without enlarging the visual glyphs. This is a proposed accessibility improvement. Do not force all typography into a scaled bitmap. At large system text sizes, allow the description to grow and, when needed, reduce visible neighbor rows rather than clipping essential information.

The comparison panel sits beside the card on wide screens and stacks below on narrow ones. It belongs to the demo. The card itself does not gain a wide-screen alternate composition.

## Color and surface tokens

Page `#F5F6F2`; primary page text `#273D31`; card `#FFFFFF`; component ink `#263633`; secondary description `#707A75`; scene labels `#FFFFFF`; active dot/progress accent `#E5F5B6`; selected pagination `#3D5E4E`; inactive pagination `#DEE4DF`.

Preserve the stable scene wash and radial readability layer from `prototype/source/rolodex.js`. Translate CSS colors carefully: CSS eight-digit hex is RRGGBBAA, while XAML color literals commonly use AARRGGBB. Do not paste alpha-bearing CSS hex values into XAML unchanged.

Use the supplied SVG strokes or equivalent XAML paths. They are 24-unit vector geometry with approximately 1.45-unit rounded strokes. No icon font is needed. Their tint is supplied by the control, not hard-coded into the SVG.

## Typography

The prototype requests Inter, Helvetica Neue, Arial, then sans-serif; it embeds no font. The packaging browser may resolve a different fallback than the user's Windows browser. Font files are deliberately not included. Resolve a properly licensed font in the app's existing design system, record the exact family/version, and compare the length and baseline of Philippines and New Zealand. Font choice remains a visual-parity gate, not an excuse to redesign the labels.

## Reference capture caveat

`reference/layout-*-preview-only.png` comes from the real HTML layout with its bundled Philippines loading crop, not a full-size source photograph. Judge control placement and hierarchy from these files. Judge motion from the live prototype and shader source after the originals are downloaded. Do not use the old QA screenshots or synthetic QA fixtures as photographic art direction.
