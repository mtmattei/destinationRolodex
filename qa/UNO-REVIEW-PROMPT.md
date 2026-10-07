# Independent Uno parity review

Inspect the implemented Uno card and the approved HTML side by side. Do not judge from screenshots alone. Read the decision register to distinguish original behavior from proposed hardening.

Run A01–A28 with actual input. Capture the two effects at fixed progress and in both directions, then capture complete playback. Verify that the text is real XAML, the photo shader actually displaces samples, both endpoints share cover/focal/camera mapping with idle rendering, and no discarded mode is exposed. Confirm that desktop success has not been presented as mobile or browser success.

Inspect draw-loop allocations, shader compilation frequency, decoded-image ownership, stale async results, pointer-capture cancellation, hidden/unloaded frame callbacks, reduced motion, text scale, touch targets and screen-reader semantics. Exercise two independent instances. Report blockers first, then visual deltas and polish issues, each with file/line, reproduction, expected behavior and concrete fix. Cite measured traces/screenshots and mark anything unverified. Do not approve because the demo looks convincing at rest.
