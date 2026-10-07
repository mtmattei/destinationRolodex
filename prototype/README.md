# Approved HTML reference

Open `approved.html` directly; all code and the original small loading preview are embedded. The six full-size photos remain external. Only Morph Blend and Liquid Warp can be selected. The original design, picker, descriptions, camera and tour remain.

`source/` is the editable implementation. Run `python tools/build_prototype.py` from the handoff root to rebuild. Running the photo downloader and then `python tools/build_prototype.py --offline` creates a separate `approved-offline.html` with the real photographs embedded.

The comparison panel is a demo/debug facility, not required production UI. Older experiments remain only in `archive/`. `qa/motion-fixtures.html` is the intentionally synthetic offline renderer test and is not a travel-photo demo.
