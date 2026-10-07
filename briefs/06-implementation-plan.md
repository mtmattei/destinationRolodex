# 06 · Implementation plan and gates

## Stage 0 · Inspect, freeze and resolve

Read the handoff, existing repo rules and available Uno skills. Run the HTML before coding. Confirm target heads, renderer choice, installed .NET/Uno/SkiaSharp versions, resource ownership strategy and font availability. Resolve or explicitly log the assumptions in the decision register. Do not change working application architecture solely to match a sample template.

For a fresh app, the official template documents .NET, presentation and markup flags. An illustrative command is:

```sh
dotnet new unoapp -o DestinationRolodex -preset blank -tfm net10.0 -presentation mvvm -markup xaml
```

Inspect `dotnet new unoapp --help` on the installed template, select the intended platforms/renderers explicitly, and record the resolved command. The command is a scaffold starting point, not proof that the desired shader surface is enabled. Verify current template installation through official Uno guidance rather than pinning an invented package patch. [S4]

**Gate:** record target/runtime choices and confirm the prototype can load the original photo pair on the build machine. If images cannot be retrieved, state the media blocker and keep synthetic fixtures outside production assets.

## Stage 1 · Prove the motion first

Create a minimal render surface with two images, a progress slider and the two approved effect modes. Port the supplied GLSL equations through the SkSL candidate. Resolve installed runtime-effect API overloads. Check surface support before construction. Render each effect at 0/.25/.5/.75/1, both directions, including mismatched image aspect ratios and the configured focal points.

**Gate:** shaders compile; static and effect endpoints align; there are no black borders; direction and liquid origin match; initial Android/Desktop capability checks are recorded. Do not spend time polishing a UI around an unsupported shader path.

## Stage 2 · Create the reusable shell

Implement the card in XAML using named resources. Keep the photo surface below real text/buttons. Build the white detail panel, topbar, controls, counters and markers. Load the existing copy/data. Use the actual card measurements, not the generated image concepts. Validate layout at 390×844 and a desktop viewport around 1440×1040.

**Gate:** component placement and typography match the reference, all text remains accessible, no horizontal overflow, no fixed description height clipping.

## Stage 3 · Implement the circular picker

Add the continuous-position model, nine recycled rows, spring settle, positive modulo and JS-compatible rounding. Implement touch/pointer capture, wheel normalization, click-to-select, keyboard navigation and indicators. Unify all commands through the same selection authority.

**Gate:** first↔last wrap works in both directions; rapid input settles on the correct country; 0/1/many-item collections behave deliberately; two instances are independent.

## Stage 4 · Integrate media and transitions

Add async decode, resource leases, latest-generation cancellation and bounded caching. Wire source/target identities to the transition coordinator and render state. Ensure old results cannot mount after a new selection. Implement the explicit loading/error/retry and fallback states. Add replay/seek in the demo layer and keep the two-effect comparison fixed to the same pair.

**Gate:** logical selection, description and loaded-photo identity are coherent; stale-photo display is labeled or replaced by the selected preview; the effect hands back without a crop/brightness jump; unsupported rendering never claims to be Morph Blend.

## Stage 5 · Camera, autoplay and lifecycle

Add pointer parallax, bounded photo exploration, zoom/reset and keyboard camera control. Match overscan and cover sampling. Add explicit play/pause, progress and hidden/occluded handling. Stop active subscriptions and release owned resources when unloaded. Implement motion reduction before performance polish.

**Gate:** no exposed image borders; Look around preserves destination; Escape restores focus; reducing motion removes decorative movement; no runaway work offscreen or after page closure.

## Stage 6 · QA and parity pass

Run the acceptance matrix, capture complete UI and raw shader frames, compare with the browser reference using the downloaded originals, and record target-specific differences. Profile warm and cold paths separately. Validate text scaling, contrast, screen reader labels and touch targets. Inspect every source photograph and its rights metadata.

**Gate:** attach actual test logs, fixed-progress captures and short playback recordings. A compilation success is not visual acceptance. A desktop success is not an Android/iOS/WebAssembly claim.

## Stage 7 · Integration handoff

Expose the public component API, add demo usage with two independent instances, document resources and renderer requirements, and retain a debug-only effect panel. Package the six original photos in the app for the offline demo. Pin versions through the repo's normal mechanism. Provide build/run instructions, known issues and exact tested target configurations.

**Completion:** the component is interactive on agreed targets; both effects are demonstrably running; offline assets and accessibility paths are verified; no placeholders, dead controls, WebView stand-in, false platform claims or unreported blockers remain.
