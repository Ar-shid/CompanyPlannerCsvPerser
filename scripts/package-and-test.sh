#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_DIR="$ROOT_DIR/src/CompanyPlannerCsvPerser"
RID="${1:-osx-arm64}"
OUTPUT_ROOT="$ROOT_DIR/artifacts/package-test-$RID"
PUBLISH_DIR="$OUTPUT_ROOT/publish"
DIST_DIR="$OUTPUT_ROOT/dist"
ZIP_PATH="$OUTPUT_ROOT/package.zip"
UNZIP_DIR="$OUTPUT_ROOT/unzipped"

echo "==> Packaging for $RID"

rm -rf "$OUTPUT_ROOT"
mkdir -p "$PUBLISH_DIR" "$DIST_DIR"

cp "$PROJECT_DIR/appsettings.Release.json" "$PROJECT_DIR/appsettings.json"

dotnet publish "$PROJECT_DIR/CompanyPlannerCsvPerser.csproj" \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -o "$PUBLISH_DIR"

if [ "$RID" = "osx-arm64" ]; then
  NODE_PATH="$PUBLISH_DIR/.playwright/node/darwin-arm64/node"
else
  NODE_PATH="$PUBLISH_DIR/.playwright/node/win32_x64/node.exe"
fi

if [ ! -f "$NODE_PATH" ]; then
  echo "Publish output is missing Playwright driver: $NODE_PATH"
  find "$PUBLISH_DIR/.playwright" -type f | head -20 || true
  exit 1
fi

cp -R "$PUBLISH_DIR/." "$DIST_DIR/"
cp -R "$PUBLISH_DIR/.playwright" "$DIST_DIR/playwright-bundle"
cp "$ROOT_DIR/distribution/README-CLIENT.txt" "$DIST_DIR/README.txt"
rm -f "$DIST_DIR"/*.pdb

if [ "$RID" = "osx-arm64" ]; then
  chmod +x "$DIST_DIR/CompanyPlannerCsvExporter"
  chmod +x "$DIST_DIR/.playwright/node/darwin-arm64/node"
  chmod +x "$DIST_DIR/playwright-bundle/node/darwin-arm64/node"
fi

echo "==> Creating zip and re-extracting (simulates client download)"
rm -f "$ZIP_PATH"
(
  cd "$DIST_DIR"
  zip -qr "$ZIP_PATH" .
)

rm -rf "$UNZIP_DIR"
mkdir -p "$UNZIP_DIR"
unzip -q "$ZIP_PATH" -d "$UNZIP_DIR"

if [ ! -d "$UNZIP_DIR/playwright-bundle" ]; then
  echo "Zip extraction is missing playwright-bundle/"
  ls -la "$UNZIP_DIR"
  exit 1
fi

echo "==> Validating extracted package"
if [ "$RID" = "osx-arm64" ]; then
  chmod +x "$UNZIP_DIR/CompanyPlannerCsvExporter"
fi

"$UNZIP_DIR/CompanyPlannerCsvExporter" --validate-package

echo
echo "Package test passed."
echo "Test folder: $UNZIP_DIR"
echo "Zip file:    $ZIP_PATH"
