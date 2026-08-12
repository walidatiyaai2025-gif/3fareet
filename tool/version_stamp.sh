#!/usr/bin/env bash
set -euo pipefail

sha="${GITHUB_SHA:-$(git rev-parse HEAD)}"
short_sha="${sha:0:8}"
run_number="${GITHUB_RUN_NUMBER:-0}"

printf 'BUILD_COMMIT=%s\n' "$sha"
printf 'BUILD_SHORT_SHA=%s\n' "$short_sha"
printf 'BUILD_NUMBER=%s\n' "$run_number"
