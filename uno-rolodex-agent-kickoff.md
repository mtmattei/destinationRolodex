# Build instruction · Destination Rolodex for Uno Platform

Rebuild the supplied working Destination Rolodex HTML as a reusable Uno Platform component. Preserve the approved card; do not redesign it. Implement only Morph Blend and Liquid Warp. Produce real XAML/C# behavior and genuine procedural photo transitions, not screenshots, a WebView wrapper or an opacity fade relabeled as morph.

## Read before changing files

Read this handoff's README, briefs 01–08, `design/design.graph.json`, `design/tokens.json`, `assets/manifest.json`, the approved component/shader source, and the target repository's own instructions. Use available Uno development skills/documentation. Inspect `prototype/approved.html` in a browser and exercise both modes, replay, scrubbing and normal navigation. The generated image concepts are not the design reference.

Honor an existing Uno solution and its conventions. For a fresh solution, propose XAML, C#, MVVM and a compatible .NET 10 template. Record actual SDK/package versions and selected renderer/targets rather than inventing a latest patch number. Do not modify global tools, existing project architecture or package versions unnecessarily.

## First milestone: shader feasibility

Before building the complete card, implement a minimal two-photo render spike. Start with the supplied GLSL and SkSL candidate. Investigate SKCanvasElement + SKRuntimeEffect, check actual target support before constructing the surface, and compile against installed APIs. Keep all text/controls in XAML above the photo surface. Do not assume Windows App SDK and Skia Desktop on Windows are interchangeable.

Show both modes at progress 0/.25/.5/.75/1 and both directions. Match cover crop, focal point, liquid origin, endpoint handoff and opaque output. Report any capability blocker with exact target/API/error evidence. Do not hide it by switching to a static image or WebView.

## Build in stages

Follow `briefs/06-implementation-plan.md`: feasibility → reusable XAML shell → circular picker → media/transition integration → camera/autoplay/lifecycle → visual/accessibility/performance QA. Commit or otherwise checkpoint stages according to the repo workflow. Keep the demo comparison panel separate from the production component. Preserve two independent card instances without shared globals.

Use positive modulo, JavaScript-compatible rounding, bounded row recycling and unified selection state. Frame progress should not trigger broad MVVM notifications. Decode media outside rendering, compile effects once, use bounded caches and dispose owned resources. Ensure stale async loads cannot win and visible photo identity is explicit during loading/retargeting.

## Asset gate

The six original full-size photos are not bundled in this handoff. Run its fetch tool on a connected machine and inspect the results. Package actual originals for the offline demo. Preserve photo IDs/credits/focal points. Keep any synthetic textures confined to test fixtures and clearly report unresolved asset/rights issues. Do not treat the supplied reference crop as a cleared final photo. No font files are supplied; record the target app's chosen typeface.

## Acceptance

Run every applicable criterion A01–A28. Provide actual recordings of Morph Blend and Liquid Warp, fixed-progress captures, both wrap directions, keyboard and touch input, rapid selection, loading/error/retry, Look around, reduced motion, no-shader fallback, lifecycle cleanup and offline startup. Report actual targets tested and measured performance. Label all untested configurations as untested.

Do not stop at compilation, a screenshot or a mostly styled card. The result must be usable and both effects visibly real. When an external prerequisite is blocked, complete unaffected work and report the precise blocker, evidence, remaining work and recovery step; do not fabricate completion.

Final delivery: changed-file summary, build/run commands, resolved versions/targets, component API example, test/recording locations, known deviations and remaining release gates. Do not add booking/account/map features or the discarded transitions.
