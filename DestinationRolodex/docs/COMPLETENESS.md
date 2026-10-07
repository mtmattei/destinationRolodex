# Completeness audit: Destination Rolodex (2026-10-07, Uno.Sdk 6.7.30, heads: net10.0-desktop, net10.0-android, net10.0-browserwasm)

Verdict: **Complete with gaps**
Safe to show (desktop): browse by wheel/drag/keys/dots, both effects, replay and scrub, Look around, tour, reduced motion,
loading/failure/retry, second card. Keep off screen: Android and WebAssembly runtime (not proven), high-DPI, screen readers.

Runtime proof in this audit comes from the DEBUG harness (deterministic start states + `RenderTargetBitmap` captures,
JSON statistics, offscreen renders through the production renderer) and the unit tests, because the App MCP was not
available in the authoring session (not rooted in the solution). Items proven that way are graded `R` with the file named;
pointer and keyboard paths that only the App MCP or a hand can drive are graded `C` and listed under *Not checked*.

## Inventory

Screens: 1 intended (demo page with the card and the motion lab) + 1 dialog (photo credits); 2 present; 0 unreachable; 0 missing
(source: briefs 01/02, design/design.graph.json). Plus 1 DEBUG-only diagnostic (`SpikePage`, `--spike`), not a product screen.
Routes: 0 registered (single page, no Navigation extension by design, brief 04); triggers: none.
Interactions: 24 found (scene wheel/drag/click, picker keys + type-ahead, play/pause, look toggle, zoom −/+/reset, retry,
credit link, 6 dots, prev/next, 2 effect radios, 3 sliders, replay, 2 demo toggles, credits link, R/1/2 keys);
0 dead by construction; 0 simulated.
Critical workflows:
1. Browse: wheel/drag/keys/dots/prev-next change the selected country, copy and photo with the configured effect, wrapping both ways.
2. Compare effects: choose Morph or Liquid, tune duration/intensity, replay the same pair, scrub to a fixed frame.
3. Look around: enter, pan/zoom/reset, Escape back; same destination throughout.
4. Tour: play/pause, 5.5 s cadence, manual input pauses; reduced motion disables it.
5. Media resilience: loading surface, failure with Retry, retry recovers.
6. Two independent cards on one page.

## Completeness by area

| # | Area | Status | Evidence | Blockers | Majors | Minors | Note |
|---|---|---|---|---|---|---|---|
| 1 | Screens | Complete | R | 0 | 0 | 0 | `captures/desktop-rest.png`, `mobile-rest.png`; credits dialog built from the catalog (`MainPage.xaml.cs` `OnOpenCredits`, C) |
| 2 | Navigation | N/A | — | 0 | 0 | 0 | Single page by design (brief 04); Escape/Back-to-list are in-card view modes (`captures/desktop-look.png`) |
| 3 | Interactions | Partial | R/C | 0 | 0 | 1 | Programmatic paths proven (`--select`, `--seek`, `--look`, `--steps`); wheel/drag/keyboard logic proven in `ControllerTests.cs` (12 tests) but not through real input |
| 4 | Logic | Complete | R | 0 | 0 | 0 | Circular math, spring, latest-target queue, camera clamps: 39 unit tests; 1,000-step loop `captures/desktop-steps.png.json` |
| 5 | Data | Complete | R | 0 | 0 | 0 | Packaged catalog + six originals load through `ms-appx` (`captures/*`); duplicate ids throw (`DestinationRepository.Parse`, C) |
| 6 | States | Complete | R | 0 | 0 | 0 | Loading `desktop-loading.png`, Failed + Retry `desktop-failed.png`, Empty `desktop-empty.png`, Single `desktop-single.png`, Ready everywhere |
| 7 | Forms | N/A | — | 0 | 0 | 0 | No forms; the lab sliders are demo controls (bound two-way, `MainPage.xaml`) |
| 8 | Authentication | N/A | — | 0 | 0 | 0 | No accounts by design (brief 01: out of scope) |
| 9 | Responsive/adaptive UI | Complete | R | 0 | 0 | 0 | 1440×1040 and 390×844 captures; lab stacks below 721 px (`{utu:Responsive}`), card radius 23 narrow, scene height clamps |
| 10 | Platform behavior | Partial | R (desktop) / X | 0 | 2 | 0 | Desktop proven; Android builds (0 warnings) but runtime parked by the user; WebAssembly: see `docs/evidence/wasm` once run |
| 11 | Failure handling | Complete | R | 0 | 0 | 0 | Decode failure → status + Retry (`--media-fail`), retry invalidates and reloads (`Retry()`); shader compile failure → PlainImage path (`desktop-noshader.png`); no-surface → XAML Image (C) |
| 12 | Accessibility | Partial | C | 0 | 0 | 2 | Names on every control, single selection surface, polite announcer, 44 px hit regions (dots 28×44); no screen-reader or large-text run |
| 13 | Performance | Complete | R (desktop) | 0 | 0 | 1 | Loop p50 15.6 ms / p95 16.9 ms with effects (`captures/perf.json`); cache bounded (6 entries, 31.8 MB); Release not profiled |
| 14 | Security/privacy | Complete | C | 0 | 0 | 0 | No network, no secrets, no storage; external links only through `Launcher.LaunchUriAsync` to credited source pages |
| 15 | Visual quality | Complete | R | 0 | 0 | 1 | Matches `browser-reference/card-desktop.png`; known deltas: row blur, text shadow, mask fold, kerning (SPEC.md) |
| 16 | Testing | Partial | R | 0 | 0 | 1 | 39 unit tests green; fixture export + browser parity + capture matrix scripted; no UI-test project |

## Critical workflows by head

| Workflow | desktop | android | wasm |
|---|---|---|---|
| 1. Browse | R (`--select`, `--steps=1000` → index 4, `desktop-liquid-050.png` rows + copy + photo) | X (parked by user) | X (not yet run) |
| 2. Compare effects | R (`--mode=liquid --seek=0.5`, fixtures both modes/directions, parity report) | X | X |
| 3. Look around | R (`desktop-look.png`, `mobile-look.png`; Escape path C) | X | X |
| 4. Tour | R (`desktop-autoplay-mid.png` ring; pause-on-input C) | X | X |
| 5. Media resilience | R (`desktop-loading.png`, `desktop-failed.png`; Retry C) | X | X |
| 6. Two cards | R (`desktop-second.png`, `mobile-second.png`) | X | X |

## Findings

### Blockers
- none

### Majors
- [Platform behavior] Android runtime not proven - head builds clean - evidence X - user parked Android for now - owner: uno-verify
- [Platform behavior] WebAssembly runtime not proven at audit time - run `python -I tools/wasm_smoke.py` after `dotnet build -f net10.0-browserwasm` - owner: uno-wasm-pwa

### Minors
- [Interactions] Real wheel/drag/keyboard input not driven - `DestinationRolodexCard.xaml.cs` input handlers - evidence C - logic covered by controller tests - owner: uno-verify (App MCP, rooted session)
- [Accessibility] Screen reader, large text and contrast not run - `CountryPicker.xaml`, `DestinationRolodexCard.xaml` - evidence C - owner: winui-xaml
- [Accessibility] Dot hit regions 28×44 (row width) - `DestinationRolodexCard.xaml.cs` `RebuildIndicators` - evidence C
- [Performance] Release configuration not profiled - owner: uno-verify
- [Visual quality] Substituted details (row blur, text shadow, mask fold, kerning) - SPEC.md capability inventory - evidence R (`desktop-rest.png`)
- [Testing] No UI-test project - owner: uno-app-ui-testing

## Not checked (X)
- Android runtime - parked by the user - `dotnet build -f net10.0-android -t:Run` with the `chefs36` AVD.
- WebAssembly runtime - build was pending at audit time - `tools/wasm_smoke.py`.
- High-DPI (150 %/200 %) - captures at 100 % only - rerun the matrix on a scaled display.
- Hidden-window behaviour (A19) - `XamlRoot.IsHostVisible` gating read in code - minimise the window during autoplay and check no advance.
- Escape focus return, retry button activation, Play while focused - need pointer/keyboard injection - App MCP in a rooted session.

## Noted for other skills
- uno-audit: lint hits are all under `xaml-lint: allow` headers (tokens over photography, control implementation, container queries); no dead code found.
- gold-standard-pass: compare tracking/kerning of country rows against `browser-reference/card-desktop.png` once the font decision is confirmed.

## Unresolved Questions
- N/A rows: Navigation (single page), Forms (none), Authentication (out of scope) - confirm these stay out of the component.
- Derived workflow 6 (two independent cards) is from the kickoff's acceptance text, not a brief - confirm it is a demo requirement.
- Android (user-parked) and WebAssembly runtime remain X on supported heads.
