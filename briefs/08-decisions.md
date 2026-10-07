# 08 · Decisions, assumptions and known deltas

| ID | Decision | Status / provenance |
| --- | --- | --- |
| D01 | Keep the existing working card, not generated image concepts | User direction |
| D02 | Expose Morph Blend and Liquid Warp only | User selection |
| D03 | Real Uno XAML/C# component, no WebView replacement | Rebuild architecture recommendation aligned with stated workflow |
| D04 | MVVM, .NET 10-compatible template | Working implementation assumption; inspect existing repo first |
| D05 | Skia-backed photo surface with runtime effects | Recommended; target support and SkSL compilation must pass a spike |
| D06 | Default 1200 ms and .60; Morph first | Existing HTML defaults, not newly user-tuned values |
| D07 | Demo tuning controls remain separate from production card | Packaging/architecture decision |
| D08 | Keep plain-image capability and reduced-motion paths internally | Accessibility/reliability requirement, not another selected effect |
| D09 | Six original full photos need download | Known packaging blocker; manifest marks all missing |
| D10 | Initial reference and crop are inspiration-only | Rights not independently established |
| D11 | Text does not morph | Observed working HTML; generated concepts are not authority |
| D12 | Pan/zoom/tilt is flat-image exploration | Observed implementation, not 360° or 3D reconstruction |
| D13 | First validate Windows Skia Desktop + Android, then Wasm | Proposed validation order; user has not fixed this component's target matrix |
| D14 | C# references and SkSL candidate are not compiled in this kit | Explicit verification boundary |
| D15 | Expand touch hit areas, support scaled text | Proposed improvements over literal CSS dimensions |
| D16 | Loading must not silently mislabel a stale photograph | Proposed correction to a prototype limitation |
| D17 | Latest-target bounded transition policy first | Proposed integration decision; no guarantee of snapshot-continuous retargeting |
| D18 | Bounded cache and lifecycle cleanup | Proposed hardening beyond the URL-cache prototype |
| D19 | Font family and exact weight must be resolved in target app | Not bundled; screenshot browser fallback is not a typography approval |
| D20 | Production photos retain original URLs/credits and country identities | Preserved source data; no unsignaled substitutions |

## What was deliberately not changed

The handoff's approved HTML retains the original card styling, spring/picker behavior, camera, descriptions, media loading, shader math and defaults. It removes the Orbit/Slice UI and renderer branches, removes the public crossfade comparison mode, limits keyboard shortcuts to 1/2, updates explanatory copy and adds build/asset tooling. Plain DOM-image fallback remains in the component. The untouched previous version is archived.

The production improvements above are instructions for the Uno rebuild; they are not quietly patched into the browser reference and presented as original behavior.

## Questions that must be resolved before release, not before starting

Which exact target heads must ship? Does Windows require the WinAppSDK renderer or is Skia Desktop acceptable? Which installed Uno/SkiaSharp combination passes the shader spike? Which font will match the approved country metrics? Are all photo licenses/credits accepted and the files downloaded? Are final duration/intensity settings still the inherited defaults? These do not require a new design round; record answers as implementation decisions as the build proceeds.
