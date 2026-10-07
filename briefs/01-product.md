# 01 · Product brief

## Outcome

Rebuild a reusable destination-browsing card in Uno Platform. A centered country list loops in either direction. The selected country is prominent; nearby countries recede. Each country has its own photograph, location, region and short description. Moving between countries uses either Morph Blend or Liquid Warp. Pointer movement introduces subtle photographic parallax; Look around allows constrained pan/zoom without turning the component into a navigation-heavy travel app.

The user approved the working HTML and selected these two live effects. Preserve it. The generated dark/glassy image concepts are not the approved design and are intentionally excluded from the rebuild kit.

## Scope and delivery units

**Production component:** DestinationRolodexCard with a reusable data contract, transition enum, duration/intensity parameters, selection events and accessible controls. The card includes its media scene, infinite country picker, tour control, Look around, location/description, position indicators and previous/next controls.

**Demo page:** one card, the existing light page treatment and a small development panel to switch the two effects, adjust duration/intensity, replay a fixed pair and scrub. This panel is not built into the reusable card's production layout. Keep it available for parity testing.

**Not in scope:** booking, maps, accounts, saved destinations, pricing, social proof, new page routes, AI image generation, real 360° geometry, semantic image morphing, video frame compositing, or the discarded Orbit Depth and Slice Shift effects. An image must never substitute for an interactive implementation.

## Content

Use the original six entries in `assets/destinations.json`, in this order: Philippines, Japan, Italy, Norway, Iceland, New Zealand. Large labels are countries. Location text is secondary. Do not copy the mixed city/country taxonomy, red logo, or incorrect geographic combinations from the initial inspiration image.

Default selection: Philippines. Default effect: Morph Blend. Alternate selectable effect: Liquid Warp. Duration: 1200 ms. Intensity: 0.60. These are existing prototype defaults, not a newly confirmed final tuning decision. The demonstration retains a 5500 ms tour interval; the reusable component should expose autoplay as an explicit host setting, off when motion is reduced.

## Success

A developer can place two independent card instances in a XAML page, assign their destinations and transition settings, and interact with each without global state coupling. The demo reproduces the current card proportions, country-list movement and photographic effects. Native text remains sharp during transitions and available to accessibility tooling. All imagery can be bundled for offline startup.

The minimum validation proposal is Skia Desktop on Windows plus Android, then WebAssembly. iOS/macOS require a suitable build host and a separate validation pass. This platform order is a planning assumption, not a platform promise made by the user.

## Source priority

1. Current user decision: preserve working card; Morph Blend and Liquid Warp only.
2. Approved live HTML and its source, for observed behavior and effect math.
3. These briefs for explicit porting decisions, safety corrections and acceptance rules.
4. Actual layout captures for dimensions and visual review.
5. Original image for inspiration only.

Where a brief corrects a prototype limitation, the decision register labels it as a proposed rebuild requirement rather than claiming it already works in HTML.
