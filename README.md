# Destination Rolodex · Uno Platform handoff

**Approved direction:** the existing interactive card, with **Morph Blend** and **Liquid Warp**. No redesign. No WebView substitute for the Uno implementation.

This is a source-backed rebuild kit, not a compiled Uno app. The working HTML is the behavioral reference. C# and SkSL files are implementation references that still require compilation and target-device validation.

## Start here

1. Open `prototype/approved.html`. It exposes only the two selected effects. Press 1 or 2 to switch, R to replay, or scrub the timeline. The full-size photographs still require a network connection.
2. Read `briefs/01-product.md`, `02-design.md`, `03-interaction.md`, and `04-architecture.md` in that order.
3. Put this folder next to the Uno solution and give the build agent `AGENT-KICKOFF.md`. The first milestone is a working two-photo shader spike, not a polished static card.

`START-HERE.html` is a local file index. `qa/motion-fixtures.html` is an offline working shader test with clearly labeled synthetic coordinate grids, separate from the destination-photo prototype. `reference/original-user-reference.png` is the supplied inspiration. Layout screenshots use the bundled loading preview and are explicitly labeled as preview-only references.

## What is in the kit

| Folder/file | Purpose |
| --- | --- |
| `prototype/approved.html` | Portable, live HTML reference; Morph Blend + Liquid Warp only |
| `prototype/source/` | Editable component, shader renderer, shell and comparison controls |
| `reference/` | Original image, actual HTML layout captures and provenance notes |
| `assets/destinations.json` | Six original destinations, copy, photo URLs and focal positions |
| `assets/manifest.json` | Local asset paths, intended Uno URIs, source credits, rights checks and missing-file status |
| `assets/icons/` | 15 vector icons extracted from the prototype; no icon-font dependency |
| `briefs/` | Product, design, interaction, architecture, motion, build plan, acceptance and decision briefs |
| `design/` | Tokens, semantic graph, state model and a validation schema |
| `uno-reference/` | Portable models/math, resource seeds, original GLSL and an explicitly uncompiled SkSL port candidate |
| `tools/` | Asset downloader, standalone-HTML builder and package validator |
| `qa/` | Reproducible browser smoke test, results and synthetic test fixtures |
| `archive/` | Untouched previous prototype and source ZIP, for provenance only |
| `AGENT-KICKOFF.md` | Ready-to-use agent build instruction with stages and stop gates |

## Full-size photo preparation

**Six full-size photographs are NOT bundled.** They were external URLs in the source prototype and could not be downloaded from the packaging environment. The reference PNG and existing Philippines preview are bundled. No substitute photo is presented as an original.

Run from this folder on a connected machine:

```sh
python -m pip install Pillow
python tools/fetch_assets.py
python tools/build_prototype.py --offline
python tools/validate_package.py --require-photos
```

The downloader preserves the selected photo identities, validates image decoding, normalizes EXIF orientation and JPEG encoding, records dimensions/hashes, and fails explicitly on missing media. It does not search for replacements. The resulting `prototype/approved-offline.html` embeds all six photographs and all code. For Uno, copy downloaded `assets/photographs/*.jpg` to `Assets/Destinations/` and use the manifest's `ms-appx` URIs.

Rights status is per asset. The supplied reference/preview is inspiration only, not a cleared production photo. Norway and Iceland credits are inherited from the earlier prototype; their individual source pages were not verified during packaging. See `assets/ASSET-NOTES.md`.

## Implementation decision

Keep the UI in **XAML + C# / MVVM**, with a small custom photo-rendering surface underneath real XAML text and controls. First investigate **SKCanvasElement + SkiaSharp runtime effects** on the selected Skia-rendered targets. Check support before constructing the surface; do not assume that the Windows App SDK target and Skia Desktop on Windows are the same renderer. See the official references and target gate in `briefs/04-architecture.md`.

The two modes are alternatives selected by a component property. They are not combined into one effect. Retain a reduced-motion/no-effect fallback as a capability/accessibility path, not a third advertised transition.

## Known boundaries

The morph is procedural displacement, not AI landmark matching. Look around is a constrained camera over a flat photograph, not reconstructed 3D. Video-to-video shader transitions are outside this first rebuild. Browser tests exercise the real shader using explicit local fixtures; they do not verify remote photography, Uno compilation or device performance. No font files are included.

Source precedence and every proposed improvement over prototype behavior are recorded in `briefs/08-decisions.md`.
