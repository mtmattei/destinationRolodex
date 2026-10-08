# Evidence: three runtime gotchas from the Destination Rolodex build

Session: Claude Code dc4f13dc-b01c-431e-96bb-89c8d5c37940 · 2026-10-07 · Uno.Sdk 6.7.30 (Uno.WinUI 6.7.135), SkiaSharp 3.119.2, Toolkit 9.1.3, .NET SDK 10.0.303, Windows 11 Pro 10.0.26200.

## 1. A zero-height Path geometry does not render on Uno Skia desktop

- Symptom: `<Path Data="M6 12h12" Stroke="..." StrokeThickness="1.45" Width="24" Height="24" Stretch="None"/>` (the minus glyph) drew nothing; the sibling plus glyph `M6 12h12M12 6v12` rendered. Capture: `docs/evidence/captures/desktop-look.png` from the first matrix run (camera pill shows "1.0× + ⟲" with an empty slot before the zoom value).
- Fix that held: give the geometry a sub-pixel vertical extent, `M6 12h12M12 11.995v.01` (`Styles/DestinationRolodexResources.xaml`, `RolodexIconMinus`). The later capture shows the minus.
- Class: framework behaviour, measured by capture; cause (empty geometry bounds skipped before stroking) inferred, not traced in source.

## 2. `{utu:Responsive}` evaluates against the window, so a harness that sizes an element does not get phone layout

- Symptom: `--viewport=390x844` set the page root to 390×844 and captured it with `RenderTargetBitmap`; the lab panel still took the Wide placement (Grid.Column 1), the star column collapsed and the card rendered at zero width (first `mobile-rest.png`: only the lab panel visible).
- Fix that held: resize the real window in `App.OnLaunched` with `AppWindow.Resize` (outer size + 16/39 for the Win32 frame), twice 600 ms apart (surface-lag gotcha). `AppWindow.ResizeClient` is Uno0001 on desktop and `SizeInt32` has no 2-arg constructor (`new SizeInt32 { Width, Height }`). After the fix `mobile-rest.png` stacks the lab below the card.
- Also seen: a card that computed `clamp(.58 × XamlRoot.Size.Height)` once from `SizeChanged`/`XamlRoot.Changed` kept the pre-resize height in one of three runs; re-checking `XamlRoot.Size` from the control's frame loop fixed it (`DestinationRolodexCard.Tick`).
- Class: expected Toolkit behaviour (documented as window based) plus a harness trap; measured by captures.

## 3. `RenderTargetBitmap` is Uno0001 on the WebAssembly head

- Symptom: `dotnet build -f net10.0-browserwasm` with `WarningsAsErrors` Uno0001 failed on `RenderTargetBitmap`, `RenderAsync`, `GetPixelsAsync`, `PixelWidth/PixelHeight` (`Harness/CaptureHelper.cs`), while the same code builds and works on `net10.0-desktop` (it captures `SKCanvasElement` content there, verified `docs/evidence/spike/spike-liquid-050-rtb.png`).
- Fix: `#if DEBUG && !__WASM__` around the helper and its call sites; capture WASM through Playwright screenshots instead (`tools/wasm_smoke.py`).
- Class: documented not-implemented API, measured by build output.
