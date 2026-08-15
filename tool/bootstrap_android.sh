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

MANIFEST="android/app/src/main/AndroidManifest.xml"
SPLASH_SOURCE="assets/branding/3fareet_splash.jpg"
DRAWABLE_DIR="android/app/src/main/res/drawable"
DRAWABLE_V21_DIR="android/app/src/main/res/drawable-v21"

sed -i 's/android:label="afareet_asphalt"/android:label="3Fareet"/' "$MANIFEST"

mkdir -p "$DRAWABLE_DIR" "$DRAWABLE_V21_DIR"
cp "$SPLASH_SOURCE" "$DRAWABLE_DIR/three_fareet_splash.jpg"

for target in "$DRAWABLE_DIR/launch_background.xml" "$DRAWABLE_V21_DIR/launch_background.xml"; do
  cat > "$target" <<'XML'
<?xml version="1.0" encoding="utf-8"?>
<layer-list xmlns:android="http://schemas.android.com/apk/res/android">
    <item android:drawable="@android:color/black" />
    <item>
        <bitmap
            android:gravity="fill"
            android:src="@drawable/three_fareet_splash" />
    </item>
</layer-list>
XML
done

echo 'Android scaffold generated for 3Fareet with branded splash. Run: flutter pub get && flutter build apk --debug'
