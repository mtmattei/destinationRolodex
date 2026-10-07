# Test evidence

## Browser verification

`browser-smoke.py` executes the approved HTML in Chromium and compiles/runs the real WebGL shader. **35 checks passed** in the packaging environment. `browser-results.json` lists them. Tests cover the two modes, endpoint consistency, distinct intermediate output, replay/seek, source/target coherence, looping, Look around, zoom, Escape, rapid navigation, mobile overflow, reduced motion and forced no-WebGL fallback. This is smoke coverage, not exhaustive interaction, accessibility or performance certification.

Test media are explicit synthetic coordinate-grid textures. They are not destination photographs. Fixed-progress captures and midpoint UI screenshots in `artifacts/` use those textures. `reference/` instead contains actual component layout captures using the bundled Philippines loading preview; see its metadata.

Open `motion-fixtures.html` for an offline working test using these six labeled textures and the actual approved source. It is deliberately separate from the photo prototype. A shader bug can be inspected without working photo CDNs, but texture identity does not verify real-photo composition or rights.

Reproduce:

```sh
python -m pip install playwright Pillow
python -m playwright install chromium
python qa/browser-smoke.py
```

Set `CHROMIUM_PATH` to an existing Chromium executable when appropriate. In the Linux packaging environment a virtual X display was needed for software ANGLE/WebGL:

```sh
xvfb-run -a python qa/browser-smoke.py
```

These tests are not frame-rate measurements. Software rendering, fixed waits and fixture interception are test-environment choices. External photo delivery, real-phone touch, screen readers, Uno/C# compilation, SkSL runtime execution and target-device performance were **not** verified here.

## Structural handoff checks

`python tools/validate_package.py` runs 14 consistency checks across graph IDs, ownership, provenance paths, states, assets, tokens and approved source. It reports the six known missing photo files separately. `--require-photos` turns that known asset gap into a failing exit status. The check is not a formal proof of the design or a Uno build.

`math-test-vectors.json` records circular-index/rounding cases for the native unit tests. `UNO-REVIEW-PROMPT.md` and the 28-item acceptance matrix define the post-build review. Native results must be attached by the implementer; do not relabel this browser report as native validation.
