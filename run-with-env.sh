#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

PORT="${1:-${PORT:-5250}}"
if [[ $# -gt 0 ]]; then
  shift
fi

export ASPNETCORE_URLS="http://127.0.0.1:${PORT}"

python3 - "$@" <<'PY2'
import os
import sys
from pathlib import Path

env = os.environ.copy()
for line in Path(".env").read_text().splitlines():
    line = line.strip()
    if not line or line.startswith("#") or "=" not in line:
        continue
    key, value = line.split("=", 1)
    env[key] = value

env["ASPNETCORE_URLS"] = os.environ["ASPNETCORE_URLS"]

args = [
    "dotnet",
    "run",
    "--project",
    "skillexa-backend.csproj",
    "--no-launch-profile",
    *sys.argv[1:],
]
os.execvpe("dotnet", args, env)
PY2
