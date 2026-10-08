# HANDOFF — Destination Rolodex Uno build (Living Postcard, revision 2)
Updated: 2026-10-08 09:40

## Where we are
Revision 2 of the brief (`../briefs/destination-rolodex-uno-living-postcard-brief.md`) is implemented in
`DestinationRolodex/`: one photograph behind an integrated serif caption with a warm 6 px rim; country rows drawn in the
Skia scene (solid ink + photo-in-type); the morph/liquid effect, globe, material colors, caption treatment and arrival
camera all read one `SceneFrame` from the picker position (no timed transition on selection); orthographic globe from a
Natural Earth mask; spring 115/21 with 1/120 s substeps; tour starts paused; no idle drift; counter and in-photo badge
removed; lab-only diagnostic crossfade. 58 unit tests green. Desktop evidence refreshed (`docs/evidence/captures`,
revision 1 archived in `captures-r1`). The brief's visual authority (revision-05 HTML + source zip) was **not** in the kit;
SPEC.md "Revision 2" lists the assumptions it would settle.

## Last verified state
- Build: `net10.0-desktop` Debug 0 warnings (Uno0001 as error). Android/WASM heads not rebuilt since revision 2 (built clean on revision 1).
- Runtime: desktop through the harness (24-state matrix incl. `--position` 0.25/0.5/0.75/1.5, 320/390/768/1440 widths, loading, failed, no-shader, empty, single, two cards, reduced, 1,000 steps, 12 s perf: loop p50 15.6 ms / p95 16.6 ms / max 25.4 ms). Fixtures re-exported scrim-free; browser parity re-run. App MCP not used.
- Git: master, last commit "feat: Living Postcard revision 2"; working tree clean after this handoff commit.
- Lint: all hits under `xaml-lint: allow` file headers (tokens over photography, custom-control implementation, container-width queries, shadow element); re-run `xaml-semantic-lint.ps1` to refresh counts.

## Guidance applied
| Skill / rule / decision | Applied at |
|---|---|
| Brief §3 one shared state (`q`, `p`, pair, travel) | `Rendering/SceneFrame.cs`, `DestinationRolodexCard.PresentFrame` |
| Brief §4 photo-in-type, material mixes, motion table | `Rendering/PhotoRenderer.DrawRows`, `Rendering/SceneFrame.cs` (`MaterialColors`), `Input/CountryPickerController.cs` (115/21, substeps) |
| Brief §5 globe (slerp, antipodal, missing coords) | `Rendering/GlobeRenderer.cs`, `GlobeTests.cs`, `Assets/Globe/` |
| skiasharp-uno: cache builder/blobs, one Build() per frame | `Rendering/PhotoRenderer.cs` |
| uno-runtime-gotchas: Responsive is window-based; RenderTargetBitmap WASM Uno0001; zero-height Path | `App.xaml.cs` (harness resize), `Harness/CaptureHelper.cs`, `Styles/DestinationRolodexResources.xaml` |
| uno-completeness-audit | `docs/COMPLETENESS.md` |
Read, not applied: uno-break-it, gold-standard-pass, uno-audit.

## Next actions (in order)
1. Drop `reference/destination-rolodex-living-postcard.html` + `destination-rolodex-living-postcard-source.zip` into the kit, then compare scrim/highlight/shadow/caption-ink against revision 05 and run `tools/browser_reference.py` against the new HTML (adapt the route keys if the photo URLs changed).
2. Rebuild and smoke `net10.0-browserwasm` (`tools/wasm_smoke.py`) for revision 2; rebuild `net10.0-android` (runtime parked by the user).
3. Rooted session + `uno-verify` (App MCP) for live wheel/drag/keys/dots and the analytic row hit test (LP09, LP15).
4. Decide the remaining assumptions in SPEC.md (serif choice, "caption ink" placement) with Matt.

## Open questions
- Serif: Source Serif 4 stands in for Georgia (not on every head).
- Where revision 05 applies the dark "caption ink" mix (currently only the shadow tint uses it).
- High-DPI, screen reader, high contrast not run.

## Relaunch
```powershell
cd C:\Users\Platform006\UnoProjects\destination-rolodex\DestinationRolodex
dotnet build DestinationRolodex/DestinationRolodex.csproj -f net10.0-desktop -c Debug
dotnet run --project DestinationRolodex/DestinationRolodex.csproj -f net10.0-desktop --no-build
bash tools/capture-matrix.sh            # evidence refresh (never kills other instances)
```
