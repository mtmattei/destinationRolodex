# HANDOFF — Destination Rolodex Uno build
Updated: 2026-10-07 16:50

## Where we are
The handoff kit (briefs 01–09, approved HTML) is rebuilt as an Uno Platform app in `DestinationRolodex/`: reusable
`DestinationRolodexCard` (looping picker, SkSL Morph Blend / Liquid Warp on `SKCanvasElement`, parallax, Look around,
tour, loading/failure/retry, reduced motion), MVVM demo page with the motion lab and a second independent card, DEBUG
harness, fixture export, browser parity script, WebAssembly smoke, unit tests (39 green). Desktop and WebAssembly runtime
verified; Android builds but its runtime is parked at the user's request. `docs/COMPLETENESS.md`: Complete with gaps.

## Last verified state
- Build: `net10.0-desktop` 0 warnings (Uno0001 as error); `net10.0-android` 0 warnings; `net10.0-browserwasm` 0 warnings (Debug).
- Runtime: desktop via harness captures (`docs/evidence/captures`, all 16 states + perf + 1,000 steps), fixtures and browser
  parity (`fixtures`, `browser-reference`, `recordings`); WebAssembly in headless Chromium (`docs/evidence/wasm`, desktop
  viewport proven, 390×844 still loading at the smoke's wait). App MCP not used (session not rooted in the solution).
- Git: master, last commit "feat: wasm head builds and runs; lint cleanup on demo page; viewport re-check in frame loop; completeness report".
- Lint (xaml-semantic-lint, re-run after cleanup): CARD 8 · HEX 96 · CODEBEHIND 43 · RESPONSIVE 5 · TOKENTHEME 0 · ICON 0; every remaining hit sits under a `xaml-lint: allow` file header (design tokens over photography, custom-control implementation, container-width queries).
- Note: the user's own desktop instance was left running at the end; the last desktop build went to `bin-verify/` (gitignored) so `bin/` stayed untouched. Rebuild `bin/` normally next session.

## Guidance applied
| Skill / rule / decision | Applied at |
|---|---|
| skiasharp-uno: cache `SKRuntimeShaderBuilder`, one `Build()` per frame | `DestinationRolodex/Rendering/PhotoRenderer.cs` (`_builder`, `DrawWithEffect`) |
| skiasharp-uno: `IsSupportedOnCurrentPlatform()` before construction + fallback | `Controls/DestinationRolodexCard.xaml.cs` `EnsureSurface` |
| uno-runtime-gotchas: `CharacterSpacing` no-op → per-glyph layout | `Controls/GlyphLayout.cs` |
| uno-runtime-gotchas: centered Grid collapses star columns → Stretch + MaxWidth | `MainPage.xaml` `LabLayout` |
| uno-runtime-gotchas: `RenderTargetBitmap` is Uno0001 on WASM → `#if !__WASM__` | `Harness/CaptureHelper.cs` |
| uno-scaffolding: `{utu:Responsive}` for window breakpoints | `MainPage.xaml` (`DemoLayout`, `MotionLab`) |
| briefs 03/05: JS rounding, positive modulo, latest-target policy | `Models/CircularMath.cs`, `Rendering/TransitionCoordinator.cs` |
| uno-completeness-audit | `docs/COMPLETENESS.md` |
Read, not applied: uno-break-it, gold-standard-pass, uno-audit.

## Next actions (in order)
1. Rooted session (`~/.claude/scripts/Open-RootedSession.ps1 -Path DestinationRolodex`) → `uno-verify` with the App MCP for live wheel/drag/keyboard evidence (A07, A08, A23); promote those rows to R in `docs/evidence/ACCEPTANCE.md` and `docs/COMPLETENESS.md`.
2. `python -I tools/wasm_smoke.py` after a Release wasm build for the 390×844 capture; drive Look around and a media failure in the browser.
3. Decide the SPEC.md substitutions (row blur, text shadow, mask fold, kerning) and verify the Norway/Iceland source pages.
4. Optional: `uno-break-it`, `gold-standard-pass`, `uno-audit` passes at feature-complete.

## Open questions
- Android runtime dropped by the user for now; head still compiles.
- WinAppSDK / iOS / macOS heads not scaffolded (SPEC.md).
- Release-configuration performance not measured.
- Harness captures use `RenderTargetBitmap`; the first desktop capture after a window resize can still race the resize (the card now re-checks `XamlRoot.Size` from its loop).

## Relaunch
```powershell
cd C:\Users\Platform006\UnoProjects\destination-rolodex\DestinationRolodex
dotnet build DestinationRolodex/DestinationRolodex.csproj -f net10.0-desktop -c Debug
dotnet run --project DestinationRolodex/DestinationRolodex.csproj -f net10.0-desktop --no-build
bash tools/capture-matrix.sh            # evidence refresh (does not kill other instances)
python -I tools/browser_reference.py    # parity vs the approved HTML
```
Plain exe launches need a harness switch (e.g. `--capture=...`) or the devserver, because `UseStudio()` runs in Debug.
