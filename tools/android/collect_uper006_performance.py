#!/usr/bin/env python3
"""Collect and bind UPER-006 runtime performance evidence from an Android debug candidate.

This tool collects evidence only. It never marks an APK or task Device Verified.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


DEFAULT_PACKAGE = "com.fiftysolutions.afareetunity3d"
RUNTIME_REPORT_FILE = "uper006-performance-baseline.json"
EXPECTED_RUNTIME_SCHEMA = 1
EXPECTED_EVIDENCE_ID = "UPER-006"
ENVELOPE_SCHEMA = 1
COLLECTION_VERDICT = "COLLECTED_NOT_VERIFIED"


class EvidenceError(RuntimeError):
    pass


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def validate_git_sha(value: str) -> str:
    normalized = value.strip().lower()
    if len(normalized) != 40 or any(ch not in "0123456789abcdef" for ch in normalized):
        raise EvidenceError("--git-sha must be the exact 40-character hexadecimal Git commit SHA")
    return normalized


def _require_number(report: dict[str, Any], key: str, *, minimum: float = 0.0) -> float:
    value = report.get(key)
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise EvidenceError(f"runtime report field {key!r} must be numeric")
    numeric = float(value)
    if not math.isfinite(numeric) or numeric < minimum:
        raise EvidenceError(f"runtime report field {key!r} must be finite and >= {minimum}")
    return numeric


def _require_text(report: dict[str, Any], key: str) -> str:
    value = report.get(key)
    if not isinstance(value, str) or not value.strip():
        raise EvidenceError(f"runtime report field {key!r} must be non-blank text")
    return value.strip()


def validate_runtime_report(report: dict[str, Any], *, minimum_samples: int = 300) -> dict[str, Any]:
    if not isinstance(report, dict):
        raise EvidenceError("runtime report must be a JSON object")
    if report.get("schemaVersion") != EXPECTED_RUNTIME_SCHEMA:
        raise EvidenceError(
            f"runtime report schemaVersion must be {EXPECTED_RUNTIME_SCHEMA}, got {report.get('schemaVersion')!r}"
        )
    if report.get("evidenceId") != EXPECTED_EVIDENCE_ID:
        raise EvidenceError(
            f"runtime report evidenceId must be {EXPECTED_EVIDENCE_ID!r}, got {report.get('evidenceId')!r}"
        )

    samples = report.get("samples")
    if isinstance(samples, bool) or not isinstance(samples, int) or samples < minimum_samples:
        raise EvidenceError(f"runtime report samples must be an integer >= {minimum_samples}")

    valid_timing_samples = report.get("validFrameTimingSamples")
    if isinstance(valid_timing_samples, bool) or not isinstance(valid_timing_samples, int):
        raise EvidenceError("runtime report validFrameTimingSamples must be an integer")
    if valid_timing_samples < 0 or valid_timing_samples > samples:
        raise EvidenceError("runtime report validFrameTimingSamples must be within [0, samples]")

    for key in (
        "avgFps",
        "avgFrameMs",
        "p95FrameMs",
        "worstFrameMs",
        "avgCpuMs",
        "avgGpuMs",
        "peakReservedMb",
    ):
        _require_number(report, key)

    if float(report["p95FrameMs"]) > float(report["worstFrameMs"]) + 0.0001:
        raise EvidenceError("runtime report p95FrameMs cannot exceed worstFrameMs")

    for key in (
        "capturedUtc",
        "deviceModel",
        "graphicsDeviceName",
        "operatingSystem",
        "processorType",
        "platform",
        "unityVersion",
        "appVersion",
        "qualityLevel",
    ):
        _require_text(report, key)

    for key in ("graphicsMemoryMb", "systemMemoryMb", "processorCount", "screenWidth", "screenHeight"):
        _require_number(report, key)

    return report


def run_adb(adb: str, serial: str | None, args: list[str]) -> subprocess.CompletedProcess[str]:
    command = [adb]
    if serial:
        command.extend(["-s", serial])
    command.extend(args)
    completed = subprocess.run(
        command,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if completed.returncode != 0:
        rendered = " ".join(command)
        raise EvidenceError(
            f"ADB command failed ({completed.returncode}): {rendered}\n"
            f"stdout={completed.stdout.strip()}\nstderr={completed.stderr.strip()}"
        )
    return completed


def resolve_serial(adb: str, requested_serial: str | None) -> str:
    if requested_serial:
        state = run_adb(adb, requested_serial, ["get-state"]).stdout.strip()
        if state != "device":
            raise EvidenceError(f"ADB serial {requested_serial!r} is not in device state: {state!r}")
        return requested_serial

    lines = run_adb(adb, None, ["devices"]).stdout.splitlines()
    devices = []
    for line in lines[1:]:
        fields = line.split()
        if len(fields) >= 2 and fields[1] == "device":
            devices.append(fields[0])
    if len(devices) != 1:
        raise EvidenceError(
            f"exactly one connected ADB device is required when --serial is omitted; found {len(devices)}"
        )
    return devices[0]


def pull_runtime_report(adb: str, serial: str, package: str) -> dict[str, Any]:
    if not package.strip():
        raise EvidenceError("package id must be non-blank")
    completed = run_adb(
        adb,
        serial,
        ["shell", "run-as", package, "cat", f"files/{RUNTIME_REPORT_FILE}"],
    )
    payload = completed.stdout.strip()
    if not payload:
        raise EvidenceError(
            f"runtime report {RUNTIME_REPORT_FILE!r} was empty; run the debug candidate long enough for 300 samples"
        )
    try:
        report = json.loads(payload)
    except json.JSONDecodeError as exc:
        raise EvidenceError(f"runtime report is not valid JSON: {exc}") from exc
    return report


def build_envelope(
    *,
    report: dict[str, Any],
    git_sha: str,
    apk_path: Path,
    apk_sha256: str,
    device_serial: str,
    package: str,
) -> dict[str, Any]:
    return {
        "schemaVersion": ENVELOPE_SCHEMA,
        "evidenceId": EXPECTED_EVIDENCE_ID,
        "verdict": COLLECTION_VERDICT,
        "collectedUtc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "candidate": {
            "gitSha": git_sha,
            "apkFile": apk_path.name,
            "apkSha256": apk_sha256,
            "package": package,
        },
        "device": {
            "adbSerial": device_serial,
            "reportedModel": report.get("deviceModel"),
            "reportedGpu": report.get("graphicsDeviceName"),
            "reportedOs": report.get("operatingSystem"),
        },
        "runtimeReport": report,
        "verificationBoundary": (
            "Collection only. This file does not satisfy physical-device acceptance, owner approval, "
            "UPER-006 completion, Last Verified APK, or publication approval by itself."
        ),
    }


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apk", type=Path, required=True, help="Exact APK installed on the target device")
    parser.add_argument("--git-sha", required=True, help="Exact 40-character Git SHA used to build the APK")
    parser.add_argument("--serial", help="ADB device serial; omit only when exactly one device is connected")
    parser.add_argument("--package", default=DEFAULT_PACKAGE)
    parser.add_argument("--adb", default="adb", help="ADB executable or path")
    parser.add_argument("--output", type=Path, default=Path("uper006-performance-evidence.json"))
    parser.add_argument("--expected-apk-sha256", help="Optional exact expected APK SHA-256")
    parser.add_argument("--minimum-samples", type=int, default=300)
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    try:
        apk_path = args.apk.resolve()
        if not apk_path.is_file():
            raise EvidenceError(f"APK does not exist: {apk_path}")
        if apk_path.suffix.lower() != ".apk":
            raise EvidenceError(f"--apk must point to an .apk file: {apk_path}")
        if args.minimum_samples <= 0:
            raise EvidenceError("--minimum-samples must be positive")

        git_sha = validate_git_sha(args.git_sha)
        apk_sha256 = sha256_file(apk_path)
        if args.expected_apk_sha256:
            expected = args.expected_apk_sha256.strip().lower()
            if expected != apk_sha256:
                raise EvidenceError(
                    f"APK SHA-256 mismatch: expected {expected}, actual {apk_sha256}"
                )

        serial = resolve_serial(args.adb, args.serial)
        report = validate_runtime_report(
            pull_runtime_report(args.adb, serial, args.package),
            minimum_samples=args.minimum_samples,
        )
        envelope = build_envelope(
            report=report,
            git_sha=git_sha,
            apk_path=apk_path,
            apk_sha256=apk_sha256,
            device_serial=serial,
            package=args.package,
        )

        output = args.output.resolve()
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(json.dumps(envelope, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(
            "AFAREET_UPER006_PERFORMANCE_EVIDENCE_COLLECTED "
            f"output={output} gitSha={git_sha} apkSha256={apk_sha256} serial={serial} "
            f"verdict={COLLECTION_VERDICT}"
        )
        return 0
    except EvidenceError as exc:
        print(f"AFAREET_UPER006_PERFORMANCE_EVIDENCE_BLOCKED error={exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
