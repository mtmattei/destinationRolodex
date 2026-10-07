#!/usr/bin/env python3
"""Stage 6 parity reference: runs the approved HTML in headless Chromium with the six packaged photographs served
in place of the remote URLs, and captures (a) raw WebGL shader frames at fixed progress for both modes and both
directions, cropped from the overscanned layer to the visible 480x530 scene, (b) full-page layout captures at
1440x1040 and 390x844. Then it compares (a) against the Uno fixture export and writes a report + composites.

Usage: python -I tools/browser_reference.py   (from the solution folder; needs playwright + Pillow)
"""
from __future__ import annotations
import base64, io, json, os, shutil, sys
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw

SOLUTION = Path(__file__).resolve().parents[1]
KIT = SOLUTION.parent
OUT = SOLUTION / "docs" / "evidence" / "browser-reference"
OUT.mkdir(parents=True, exist_ok=True)
FIXTURES = SOLUTION / "docs" / "evidence" / "fixtures" / "fixed"
KEYS = {"1758782551890": "ph", "1635861105962": "jp", "1741893041975": "it", "1710762635726": "no", "1532586733327": "is", "Milford": "nz"}
PHOTOS = {k: (KIT / "assets" / "photographs" / f"{v}.jpg").read_bytes() for k, v in KEYS.items()}
OVERSCAN = 0.13


def crop_scene(png_bytes: bytes) -> Image.Image:
    im = Image.open(io.BytesIO(png_bytes)).convert("RGB")
    w, h = im.size
    fx = OVERSCAN / (1 + 2 * OVERSCAN)
    box = (round(w * fx), round(h * fx), round(w * (1 - fx)), round(h * (1 - fx)))
    return im.crop(box).resize((480, 530), Image.LANCZOS)


def mean_diff(a: Image.Image, b: Image.Image) -> float:
    d = ImageChops.difference(a.convert("RGB"), b.convert("RGB"))
    hist = d.histogram()
    total = 0
    for ch in range(3):
        band = hist[ch * 256:(ch + 1) * 256]
        total += sum(i * c for i, c in enumerate(band))
    return total / (a.size[0] * a.size[1] * 3)


def main() -> int:
    from playwright.sync_api import sync_playwright
    html = (KIT / "prototype" / "approved.html").read_text(encoding="utf-8")
    report: dict = {"source": "prototype/approved.html in headless Chromium (ANGLE/SwiftShader)", "pairs": []}
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True, args=["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader"])
        page = browser.new_page(viewport={"width": 1440, "height": 1040}, device_scale_factor=1)

        def serve(route):
            for key, body in PHOTOS.items():
                if key in route.request.url:
                    route.fulfill(status=200, content_type="image/jpeg", headers={"Access-Control-Allow-Origin": "*"}, body=body)
                    return
            route.abort()

        page.route("https://**/*", serve)
        page.set_content(html, wait_until="load")
        page.wait_for_function("window.destinationCard && destinationCard.selectedIndex===0")
        page.evaluate("destinationCard.pause()")
        page.wait_for_timeout(1500)
        report["webgl"] = page.evaluate("({available:destinationCard._fx.available,error:destinationCard._fx.error})")
        page.screenshot(path=str(OUT / "layout-desktop-1440x1040.png"), full_page=False)
        page.locator("destination-rolodex").screenshot(path=str(OUT / "card-desktop.png"))

        def capture_pair(from_id: str, to_id: str, direction: int, name: str):
            page.evaluate("([f,t])=>{const d=destinationCard._data;destinationCard._lastTransitionPair={from:d.find(x=>x.id===f),to:d.find(x=>x.id===t)};}", [from_id, to_id])
            page.evaluate("destinationCard.transitionStrength=.6;destinationCard.transitionDuration=1200;")
            for mode in ["morph", "liquid"]:
                page.evaluate("(m)=>{destinationCard.transition=m;}", mode)
                for progress in [0, .25, .5, .75, 1]:
                    page.evaluate("(p)=>destinationCard.seekTransition(p)", progress)
                    page.wait_for_timeout(120)
                    raw = page.evaluate('destinationCard._fx.canvas.toDataURL("image/png").split(",")[1]')
                    scene = crop_scene(base64.b64decode(raw))
                    fname = f"{mode}-{name}-{int(progress*100):03}.png"
                    scene.save(OUT / fname)
                    entry = {"file": fname, "mode": mode, "pair": name, "progress": progress}
                    uno = FIXTURES / fname
                    if uno.exists():
                        u = Image.open(uno).convert("RGB")
                        entry["meanAbsDiffVsUno"] = round(mean_diff(scene, u), 2)
                        comp = Image.new("RGB", (480 * 2 + 12, 530 + 28), "white")
                        comp.paste(scene, (0, 28)); comp.paste(u, (492, 28))
                        dr = ImageDraw.Draw(comp)
                        dr.text((8, 8), f"browser WebGL  {fname}", fill="black")
                        dr.text((500, 8), f"Uno SkSL (CPU raster)  mean|diff|={entry['meanAbsDiffVsUno']}", fill="black")
                        comp.save(OUT / f"compare-{fname}")
                    report["pairs"].append(entry)
            page.evaluate("destinationCard._exitPreview()")
            page.wait_for_timeout(300)

        capture_pair("ph", "jp", 1, "ph-jp")
        capture_pair("jp", "ph", -1, "jp-ph")
        capture_pair("jp", "it", 1, "jp-it")

        page.set_viewport_size({"width": 390, "height": 844})
        page.wait_for_timeout(600)
        page.screenshot(path=str(OUT / "layout-mobile-390x844.png"), full_page=False)
        browser.close()

    (OUT / "report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
