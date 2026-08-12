#!/usr/bin/env bash
set -euo pipefail

command -v flutter >/dev/null 2>&1 || {
  echo 'Flutter is required on PATH.' >&2
  exit 1
}

TEMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TEMP_DIR"' EXIT

flutter create \
  --empty \
  --no-pub \
  --platforms=android \
  --org com.fiftysolutions \
  --project-name afareet_asphalt \
  "$TEMP_DIR/afareet_scaffold"

rm -rf android
cp -R "$TEMP_DIR/afareet_scaffold/android" ./android

echo 'Android scaffold generated. Run: flutter pub get && flutter build apk --debug'
