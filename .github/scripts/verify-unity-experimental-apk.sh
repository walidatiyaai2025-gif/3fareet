#!/usr/bin/env bash
set -euo pipefail

APK_PATH="${1:-unity_game/Builds/Android/afareet-unity3d-experimental.apk}"
EVIDENCE_DIR="${2:-artifacts/android-experimental}"
EXPECTED_PACKAGE="com.fiftysolutions.afareetunity3d"
EXPECTED_ABI="arm64-v8a"
ARTIFACT_NAME="afareet-unity3d-experimental.apk"

if [[ ! -f "$APK_PATH" || ! -s "$APK_PATH" ]]; then
  echo "::error::Experimental Android APK is missing or empty: $APK_PATH"
  exit 1
fi

mkdir -p "$EVIDENCE_DIR"

ANDROID_SDK_ROOT_RESOLVED="${ANDROID_SDK_ROOT:-${ANDROID_HOME:-}}"
if [[ -z "$ANDROID_SDK_ROOT_RESOLVED" ]]; then
  echo "::error::ANDROID_SDK_ROOT/ANDROID_HOME is unavailable on the runner."
  exit 1
fi

AAPT="$(find "$ANDROID_SDK_ROOT_RESOLVED/build-tools" -type f -name aapt -print 2>/dev/null | sort -V | tail -n 1)"
if [[ -z "$AAPT" || ! -x "$AAPT" ]]; then
  echo "::error::aapt was not found under $ANDROID_SDK_ROOT_RESOLVED/build-tools."
  exit 1
fi

BADGING="$EVIDENCE_DIR/aapt-badging.txt"
"$AAPT" dump badging "$APK_PATH" | tee "$BADGING"

grep -Fq "package: name='$EXPECTED_PACKAGE'" "$BADGING" || {
  echo "::error::APK package id is not $EXPECTED_PACKAGE"
  exit 1
}

grep -Fq "sdkVersion:'26'" "$BADGING" || {
  echo "::error::APK minSdk is not Android API 26"
  exit 1
}

mapfile -t ABIS < <(
  unzip -Z1 "$APK_PATH" \
    | sed -n 's#^lib/\([^/]*\)/.*#\1#p' \
    | sort -u
)
if [[ "${#ABIS[@]}" -ne 1 || "${ABIS[0]}" != "$EXPECTED_ABI" ]]; then
  printf '::error::Expected only ABI %s; found: %s\n' "$EXPECTED_ABI" "${ABIS[*]:-none}"
  exit 1
fi

unzip -Z1 "$APK_PATH" | grep -Fqx "lib/$EXPECTED_ABI/libunity.so" || {
  echo "::error::APK does not contain lib/$EXPECTED_ABI/libunity.so"
  exit 1
}

SHA256="$(sha256sum "$APK_PATH" | awk '{print $1}')"
SIZE_BYTES="$(stat -c '%s' "$APK_PATH")"
printf '%s  %s\n' "$SHA256" "$ARTIFACT_NAME" | tee "$EVIDENCE_DIR/$ARTIFACT_NAME.sha256"

python3 - "$EVIDENCE_DIR/artifact-metadata.json" "$EXPECTED_PACKAGE" "$EXPECTED_ABI" "$SHA256" "$SIZE_BYTES" <<'PY'
import json
import os
import sys

out, package_id, abi, sha256, size_bytes = sys.argv[1:]
payload = {
    "schemaVersion": 1,
    "source": "github-actions-unity-experimental-ci",
    "artifact": "afareet-unity3d-experimental.apk",
    "artifactClass": "experimental",
    "packageId": package_id,
    "minSdk": 26,
    "abi": abi,
    "sha256": sha256,
    "sizeBytes": int(size_bytes),
    "gitSha": os.environ.get("GITHUB_SHA", ""),
    "runId": os.environ.get("GITHUB_RUN_ID", ""),
    "runAttempt": os.environ.get("GITHUB_RUN_ATTEMPT", ""),
    "repository": os.environ.get("GITHUB_REPOSITORY", ""),
    "workflow": os.environ.get("GITHUB_WORKFLOW", ""),
    "eventName": os.environ.get("GITHUB_EVENT_NAME", ""),
    "ref": os.environ.get("GITHUB_REF", ""),
    "releaseEvidenceEligible": False,
    "physicalDeviceVerified": False,
}
with open(out, "w", encoding="utf-8") as fh:
    json.dump(payload, fh, indent=2, sort_keys=True)
    fh.write("\n")
PY

echo "AFAREET_EXPERIMENTAL_ANDROID_ARTIFACT_OK package=$EXPECTED_PACKAGE abi=$EXPECTED_ABI sha256=$SHA256 size=$SIZE_BYTES releaseEvidenceEligible=false"
