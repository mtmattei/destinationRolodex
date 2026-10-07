# HANDOFF — Destination Rolodex Uno build
Updated: 2026-10-07 16:05

## Where we are
The handoff kit (briefs 01–09, approved HTML) is rebuilt as an Uno Platform app in `DestinationRolodex/`: reusable
`DestinationRolodexCard` (looping picker, SkSL Morph Blend / Liquid Warp on `SKCanvasElement`, parallax, Look around,
tour, loading/failure/retry, reduced motion), MVVM demo page with the motion lab and a second independent card, DEBUG
harness, fixture export, browser parity script, unit tests (39 green). Desktop head verified through harness captures;
Android builds clean but the user dropped Android runtime for now; WebAssembly head not yet built. The user recorded the
desktop app at the end of the session.

## Last verified state
- Build: `net10.0-desktop` Debug 0 warnings (Uno0001 as error); `net10.0-android` Debug 0 warnings; `net10.0-browserwasm` not built.
- Runtime: desktop via DEBUG harness captures (`docs/evidence/captures`, `fixtures`, `browser-reference`, `recordings`); App MCP not used (session not rooted in the solution). Last capture matrix was interrupted after the rebuild that fixed the lab layout stretch and the slider feedback loop; re-run `bash tools/capture-matrix.sh` to refresh `desktop-*.png` (the pre-fix ones show a 420 px card and an unintended replay).
- Git: master, last commit "fix: queue ordering, user-driven lab sliders, layout stretch, harness window sizing; add evidence and docs"; working tree clean.
- Lint: xaml-semantic-lint reported HEX/CARD/CODEBEHIND/RESPONSIVE/ICON hits, all with `xaml-lint: allow` reasons at file top (tokens over photography, control implementation, container queries); full count not re-run.

## Guidance applied
| Skill / rule / decision | Applied at |
|---|---|
| skiasharp-uno: cache `SKRuntimeShaderBuilder`, one `Build()` per frame | `DestinationRolodex/Rendering/PhotoRenderer.cs` (`_builder`, `DrawWithEffect`) |
| skiasharp-uno: `IsSupportedOnCurrentPlatform()` before construction + fallback | `Controls/DestinationRolodexCard.xaml.cs` `EnsureSurface` |
| uno-runtime-gotchas: `CharacterSpacing` no-op → per-glyph layout | `Controls/GlyphLayout.cs` |
| uno-runtime-gotchas: centered Grid collapses star columns → Stretch + MaxWidth | `MainPage.xaml` `LabLayout` |
| uno-scaffolding: `{utu:Responsive}` for window breakpoints | `MainPage.xaml` (`DemoLayout`, `MotionLab`) |
| briefs 03/05: JS rounding, positive modulo, latest-target policy | `Models/CircularMath.cs`, `Rendering/TransitionCoordinator.cs` |
Read, not applied: uno-completeness-audit (report not written), uno-break-it, gold-standard-pass, uno-audit.

## Next actions (in order)
1. `bash tools/capture-matrix.sh` from `DestinationRolodex/`, then check `captures/desktop-rest.png` shows a 480 px card and no replay; refresh `docs/evidence/ACCEPTANCE.md` references.
2. `dotnet build -f net10.0-browserwasm` and run it; record runtime-effect support on the browser Skia backend in ACCEPTANCE.md A27.
3. Open a rooted session (`~/.claude/scripts/Open-RootedSession.ps1 -Path DestinationRolodex`) and run `uno-verify` for live pointer/keyboard evidence (A07, A08, A23), then `uno-completeness-audit` → `docs/COMPLETENESS.md`.
4. Decide the SPEC.md substitutions (row blur, text shadow, mask fold, kerning loss) and the Norway/Iceland source-page verification.

## Open questions
- Android runtime dropped by the user for now; head still compiles.
- WinAppSDK / iOS / macOS heads not scaffolded (SPEC.md).
- Release-configuration performance not measured.

## Relaunch
```powershell
cd C:\Users\Platform006\UnoProjects\destination-rolodex\DestinationRolodex
dotnet build DestinationRolodex/DestinationRolodex.csproj -f net10.0-desktop -c Debug
dotnet run --project DestinationRolodex/DestinationRolodex.csproj -f net10.0-desktop --no-build
```
Plain exe launches need a harness switch (e.g. `--capture=...`) or the devserver, because `UseStudio()` runs in Debug.
