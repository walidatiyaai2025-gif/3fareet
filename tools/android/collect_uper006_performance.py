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
EXPECTED_CANDIDATE_MANIFEST_SCHEMA = 1
EXPECTED_CANDIDATE_TYPE = "local-windows-licensed-unity"
EXPECTED_CANDIDATE_VERDICT = "READY_FOR_PHYSICAL_DEVICE_EVIDENCE"
ENVELOPE_SCHEMA = 1
COLLECTION_VERDICT = "COLLECTED_NOT_VERIFIED"
MANIFEST_BINDING = "LICENSED_CANDIDATE_MANIFEST"
LEGACY_BINDING = "USER_SUPPLIED_GIT_SHA_ONLY"


class EvidenceError(RuntimeError):
    pass


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def sha256_bytes(payload: bytes) -> str:
    if not payload:
        raise EvidenceError("installed APK payload was empty")
    return hashlib.sha256(payload).hexdigest()


def validate_git_sha(value: str) -> str:
    normalized = value.strip().lower()
    if len(normalized) != 40 or any(ch not in "0123456789abcdef" for ch in normalized):
        raise EvidenceError("--git-sha must be the exact 40-character hexadecimal Git commit SHA")
    return normalized


def validate_sha256(value: str, *, label: str) -> str:
    normalized = value.strip().lower()
    if len(normalized) != 64 or any(ch not in "0123456789abcdef" for ch in normalized):
        raise EvidenceError(f"{label} must be the exact 64-character hexadecimal SHA-256")
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


def load_json_object(path: Path, *, label: str) -> dict[str, Any]:
    if not path.is_file():
        raise EvidenceError(f"{label} does not exist: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise EvidenceError(f"{label} is not valid JSON: {exc}") from exc
    if not isinstance(payload, dict):
        raise EvidenceError(f"{label} must be a JSON object")
    return payload


def validate_candidate_manifest(
    manifest: dict[str, Any],
    *,
    local_apk_sha256: str,
    package: str,
) -> str:
    if manifest.get("schemaVersion") != EXPECTED_CANDIDATE_MANIFEST_SCHEMA:
        raise EvidenceError(
            "candidate manifest schemaVersion must be "
            f"{EXPECTED_CANDIDATE_MANIFEST_SCHEMA}, got {manifest.get('schemaVersion')!r}"
        )
    if manifest.get("candidateType") != EXPECTED_CANDIDATE_TYPE:
        raise EvidenceError(
            f"candidate manifest candidateType must be {EXPECTED_CANDIDATE_TYPE!r}"
        )
    git_sha = validate_git_sha(_require_text(manifest, "gitSha"))
    if _require_text(manifest, "packageId") != package:
        raise EvidenceError(
            f"candidate manifest packageId does not match requested package {package!r}"
        )
    if manifest.get("releaseEvidenceEligible") is not True:
        raise EvidenceError("candidate manifest releaseEvidenceEligible must be JSON boolean true")
    if manifest.get("readyForDeviceEvidence") is not True:
        raise EvidenceError("candidate manifest readyForDeviceEvidence must be JSON boolean true")
    if manifest.get("verified") is not False:
        raise EvidenceError("candidate manifest verified must remain JSON boolean false before device evidence")
    if manifest.get("verdict") != EXPECTED_CANDIDATE_VERDICT:
        raise EvidenceError(
            f"candidate manifest verdict must be {EXPECTED_CANDIDATE_VERDICT!r}"
        )

    apk = manifest.get("apk")
    if not isinstance(apk, dict):
        raise EvidenceError("candidate manifest apk must be a JSON object")
    declared_hash = validate_sha256(_require_text(apk, "sha256"), label="candidate manifest apk.sha256")
    if declared_hash != local_apk_sha256:
        raise EvidenceError(
            f"candidate manifest APK SHA-256 mismatch: manifest={declared_hash} local={local_apk_sha256}"
        )
    _require_text(apk, "fileName")
    _require_text(manifest, "unityVersion")
    return git_sha


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


def run_adb_bytes(adb: str, serial: str, args: list[str]) -> subprocess.CompletedProcess[bytes]:
    command = [adb, "-s", serial]
    command.extend(args)
    completed = subprocess.run(
        command,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if completed.returncode != 0:
        rendered = " ".join(command)
        stderr = completed.stderr.decode("utf-8", errors="replace").strip()
        raise EvidenceError(
            f"ADB binary command failed ({completed.returncode}): {rendered}\nstderr={stderr}"
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


def parse_installed_apk_path(pm_path_output: str, *, package: str) -> str:
    paths = []
    for raw_line in pm_path_output.splitlines():
        line = raw_line.strip()
        if not line:
            continue
        if not line.startswith("package:"):
            raise EvidenceError(f"unexpected pm path output for {package!r}: {line!r}")
        path = line[len("package:"):].strip()
        if not path:
            raise EvidenceError(f"pm path returned an empty APK path for {package!r}")
        paths.append(path)

    if not paths:
        raise EvidenceError(f"package {package!r} is not installed on the selected device")
    if len(paths) != 1:
        raise EvidenceError(
            f"package {package!r} is installed as {len(paths)} split APKs; UPER-006 exact standalone APK evidence requires one base APK"
        )
    if not paths[0].lower().endswith(".apk"):
        raise EvidenceError(f"installed package path is not an APK: {paths[0]!r}")
    return paths[0]


def resolve_installed_apk_path(adb: str, serial: str, package: str) -> str:
    completed = run_adb(adb, serial, ["shell", "pm", "path", package])
    return parse_installed_apk_path(completed.stdout, package=package)


def hash_installed_apk(adb: str, serial: str, installed_apk_path: str) -> str:
    if not installed_apk_path.strip():
        raise EvidenceError("installed APK path must be non-blank")
    payload = run_adb_bytes(adb, serial, ["exec-out", "cat", installed_apk_path]).stdout
    return sha256_bytes(payload)


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
    installed_apk_path: str | None = None,
    installed_apk_sha256: str | None = None,
    candidate_manifest_path: Path | None = None,
    candidate_manifest_sha256: str | None = None,
    candidate_binding: str = LEGACY_BINDING,
) -> dict[str, Any]:
    return {
        "schemaVersion": ENVELOPE_SCHEMA,
        "evidenceId": EXPECTED_EVIDENCE_ID,
        "verdict": COLLECTION_VERDICT,
        "collectedUtc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "candidateBinding": {
            "mode": candidate_binding,
            "manifestFile": candidate_manifest_path.name if candidate_manifest_path else None,
            "manifestSha256": candidate_manifest_sha256,
        },
        "candidate": {
            "gitSha": git_sha,
            "apkFile": apk_path.name,
            "apkSha256": apk_sha256,
            "package": package,
        },
        "installedApk": {
            "path": installed_apk_path,
            "sha256": installed_apk_sha256,
            "matchesCandidate": installed_apk_sha256 == apk_sha256 if installed_apk_sha256 else None,
        },
        "device": {
            "adbSerial": device_serial,
            "reportedModel": report.get("deviceModel"),
            "reportedGpu": report.get("graphicsDeviceName"),
            "reportedOs": report.get("operatingSystem"),
        },
        "runtimeReport": report,
        "verificationBoundary": (
            "Collection only. Candidate-manifest and installed-APK binding prove evidence provenance, "
            "not acceptance. This file does not satisfy physical-device acceptance, owner approval, "
            "UPER-006 completion, Last Verified APK, or publication approval by itself."
        ),
    }


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apk", type=Path, required=True, help="Exact APK installed on the target device")
    parser.add_argument(
        "--candidate-manifest",
        type=Path,
        help="Licensed local-candidate-manifest.json that binds Git SHA and APK hash",
    )
    parser.add_argument(
        "--git-sha",
        help="Legacy exact Git SHA when no candidate manifest is available; manifest binding is preferred",
    )
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
        if not args.candidate_manifest and not args.git_sha:
            raise EvidenceError("provide --candidate-manifest for exact candidate binding or legacy --git-sha")

        apk_sha256 = sha256_file(apk_path)
        if args.expected_apk_sha256:
            expected = validate_sha256(args.expected_apk_sha256, label="--expected-apk-sha256")
            if expected != apk_sha256:
                raise EvidenceError(
                    f"APK SHA-256 mismatch: expected {expected}, actual {apk_sha256}"
                )

        candidate_manifest = None
        candidate_manifest_path = None
        candidate_manifest_sha256 = None
        candidate_binding = LEGACY_BINDING
        if args.candidate_manifest:
            candidate_manifest_path = args.candidate_manifest.resolve()
            candidate_manifest = load_json_object(candidate_manifest_path, label="candidate manifest")
            candidate_manifest_sha256 = sha256_file(candidate_manifest_path)
            git_sha = validate_candidate_manifest(
                candidate_manifest,
                local_apk_sha256=apk_sha256,
                package=args.package,
            )
            candidate_binding = MANIFEST_BINDING
            if args.git_sha and validate_git_sha(args.git_sha) != git_sha:
                raise EvidenceError(
                    f"--git-sha does not match candidate manifest: cli={args.git_sha} manifest={git_sha}"
                )
        else:
            git_sha = validate_git_sha(args.git_sha)

        serial = resolve_serial(args.adb, args.serial)
        installed_apk_path = resolve_installed_apk_path(args.adb, serial, args.package)
        installed_apk_sha256 = hash_installed_apk(args.adb, serial, installed_apk_path)
        if installed_apk_sha256 != apk_sha256:
            raise EvidenceError(
                "installed APK SHA-256 does not match the candidate file: "
                f"installed={installed_apk_sha256} local={apk_sha256}"
            )

        report = validate_runtime_report(
            pull_runtime_report(args.adb, serial, args.package),
            minimum_samples=args.minimum_samples,
        )
        if candidate_manifest is not None:
            manifest_unity = _require_text(candidate_manifest, "unityVersion")
            if report["unityVersion"] != manifest_unity:
                raise EvidenceError(
                    f"runtime Unity version does not match candidate manifest: report={report['unityVersion']} manifest={manifest_unity}"
                )

        envelope = build_envelope(
            report=report,
            git_sha=git_sha,
            apk_path=apk_path,
            apk_sha256=apk_sha256,
            device_serial=serial,
            package=args.package,
            installed_apk_path=installed_apk_path,
            installed_apk_sha256=installed_apk_sha256,
            candidate_manifest_path=candidate_manifest_path,
            candidate_manifest_sha256=candidate_manifest_sha256,
            candidate_binding=candidate_binding,
        )

        output = args.output.resolve()
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(json.dumps(envelope, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(
            "AFAREET_UPER006_PERFORMANCE_EVIDENCE_COLLECTED "
            f"output={output} gitSha={git_sha} apkSha256={apk_sha256} serial={serial} "
            f"binding={candidate_binding} installedApkMatch=true verdict={COLLECTION_VERDICT}"
        )
        return 0
    except EvidenceError as exc:
        print(f"AFAREET_UPER006_PERFORMANCE_EVIDENCE_BLOCKED error={exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
