#!/usr/bin/env python3
"""WebAssembly smoke: serves the built wasm head (wwwroot) with dotnet-serve, loads it in headless Chromium, waits for the
app to paint, screenshots it at 1440x1040 and 390x844, and records console errors. The Uno Skia WASM renderer draws
into a canvas, so the proof is the screenshot plus the absence of console/page errors.

Usage: python -I tools/wasm_smoke.py  (from the solution folder; wasm head already built)
"""
from __future__ import annotations
import json, os, re, subprocess, sys, time
from pathlib import Path

SOLUTION = Path(__file__).resolve().parents[1]
OUT = SOLUTION / "docs" / "evidence" / "wasm"
OUT.mkdir(parents=True, exist_ok=True)
PROJECT = SOLUTION / "DestinationRolodex" / "DestinationRolodex.csproj"


def main() -> int:
    from playwright.sync_api import sync_playwright
    wwwroot = SOLUTION / "DestinationRolodex" / "bin" / "Debug" / "net10.0-browserwasm" / "wwwroot"
    # dotnet-serve (global tool) on a free port: `dotnet run` binds the launchSettings port, which may be in use.
    proc = subprocess.Popen(
        ["dotnet", "serve", "-d", str(wwwroot), "-p", "5399", "--mime", ".wasm=application/wasm", "--mime", ".dat=application/octet-stream"],
        cwd=str(SOLUTION), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, encoding="utf-8", errors="replace")
    url = "http://localhost:5399"
    import urllib.request
    deadline = time.time() + 120
    ready = False
    try:
        while time.time() < deadline and proc.poll() is None:
            try:
                with urllib.request.urlopen(url + "/index.html", timeout=3) as r:
                    if r.status == 200:
                        ready = True
                        break
            except Exception:
                time.sleep(1)
        if not ready:
            print("dotnet serve did not answer on " + url)
            return 2

        report = {"url": url, "consoleErrors": [], "pageErrors": [], "screens": []}
        with sync_playwright() as p:
            browser = p.chromium.launch(headless=True, args=["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader"])
            for name, w, h in [("wasm-desktop-1440x1040", 1440, 1040), ("wasm-mobile-390x844", 390, 844)]:
                page = browser.new_page(viewport={"width": w, "height": h}, device_scale_factor=1)
                page.on("console", lambda msg: report["consoleErrors"].append(msg.text) if msg.type in ("error",) else None)
                page.on("pageerror", lambda err: report["pageErrors"].append(str(err)))
                page.goto(url, wait_until="load", timeout=180000)
                page.wait_for_timeout(45000)
                path = OUT / f"{name}.png"
                page.screenshot(path=str(path))
                report["screens"].append(str(path.relative_to(SOLUTION)))
                page.close()
            browser.close()
        (OUT / "report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps(report, indent=2))
        return 0
    finally:
        proc.terminate()
        try:
            proc.wait(timeout=10)
        except subprocess.TimeoutExpired:
            proc.kill()
        subprocess.run(["taskkill", "/F", "/IM", "dotnet-serve.exe"], capture_output=True)


if __name__ == "__main__":
    sys.exit(main())
