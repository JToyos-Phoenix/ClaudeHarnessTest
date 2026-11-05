#!/usr/bin/env bash
# init.sh - Thin wrapper so `./init.sh` works from Bash / Git Bash / macOS / Linux.
# All the verification logic lives in init.ps1 (one source of truth).
set -euo pipefail
cd "$(dirname "$0")"

if command -v pwsh >/dev/null 2>&1; then
  exec pwsh -NoProfile -ExecutionPolicy Bypass -File ./init.ps1 "$@"
elif command -v powershell.exe >/dev/null 2>&1; then
  exec powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./init.ps1 "$@"
else
  echo "[FAIL]  PowerShell not found. Install pwsh: https://aka.ms/powershell" >&2
  exit 1
fi
