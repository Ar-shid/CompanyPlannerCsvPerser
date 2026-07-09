#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/src/CompanyPlannerCsvPerser"

dotnet build "$PROJECT_DIR/CompanyPlannerCsvPerser.csproj"

PLAYWRIGHT_PACKAGE="$PROJECT_DIR/bin/Debug/net8.0/.playwright/package"
if [[ -f "$PLAYWRIGHT_PACKAGE/cli.js" ]]; then
  node "$PLAYWRIGHT_PACKAGE/cli.js" install chromium --force
elif command -v pwsh >/dev/null 2>&1; then
  pwsh "$PROJECT_DIR/bin/Debug/net8.0/playwright.ps1" install chromium
else
  echo "Playwright installer not found. Install PowerShell (pwsh) or ensure the project is built." >&2
  exit 1
fi

echo "Playwright Chromium installed."
