#!/usr/bin/env bash
# Drives the DEBUG harness through the acceptance states and writes PNG/JSON evidence under docs/evidence/captures.
# Usage: tools/capture-matrix.sh [exe]   (run from the solution folder after a Debug desktop build)
set -u
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
EXE="${1:-$ROOT/DestinationRolodex/bin/Debug/net10.0-desktop/DestinationRolodex.exe}"
OUT="$ROOT/docs/evidence/captures"
mkdir -p "$OUT"
run() {
  local name="$1"; shift
  taskkill //F //IM DestinationRolodex.exe >/dev/null 2>&1
  local start=$(date +%s)
  timeout 75 "$EXE" --capture="$OUT/$name.png" "$@" >"$OUT/$name.log" 2>&1
  local code=$?
  taskkill //F //IM DestinationRolodex.exe >/dev/null 2>&1
  printf '%-28s exit=%s %ss\n' "$name" "$code" "$(( $(date +%s) - start ))"
}
run desktop-rest           --viewport=1440x1040 --settle=2500
run mobile-rest            --viewport=390x844   --settle=2500
run desktop-liquid-050     --viewport=1440x1040 --mode=liquid --seek=0.5 --settle=1500
run desktop-morph-050      --viewport=1440x1040 --mode=morph  --seek=0.5 --settle=1500
run desktop-look           --viewport=1440x1040 --look --settle=2500
run mobile-look            --viewport=390x844   --look --settle=2500
run desktop-loading        --viewport=1440x1040 --select=it --media-delay=30000 --settle=2500
run desktop-failed         --viewport=1440x1040 --select=jp --media-fail=jp --settle=2500
run desktop-reduced        --viewport=1440x1040 --reduced --select=jp --settle=2500
run desktop-noshader       --viewport=1440x1040 --no-shader --mode=liquid --seek=0.5 --settle=2500
run desktop-empty          --viewport=1440x1040 --empty --settle=1500
run desktop-single         --viewport=1440x1040 --single --settle=1500
run desktop-second         --viewport=1440x1040 --second --settle=2500
run mobile-second          --viewport=390x1000  --second --settle=2500
run desktop-steps          --viewport=1440x1040 --steps=1000 --settle=4000
run desktop-autoplay-mid   --viewport=1440x1040 --autoplay --settle=6000
taskkill //F //IM DestinationRolodex.exe >/dev/null 2>&1
timeout 60 "$EXE" --perf="$OUT/perf.json" --viewport=1440x1040 >"$OUT/perf.log" 2>&1; echo "perf exit=$?"
taskkill //F //IM DestinationRolodex.exe >/dev/null 2>&1
ls -la "$OUT"/*.png "$OUT"/*.json 2>/dev/null
