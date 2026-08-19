#!/usr/bin/env python3
"""Collect exact-candidate UPER-006 Android performance evidence.

The collector binds the runtime report to the licensed local candidate manifest,
the local APK bytes, and the exact APK bytes installed on the selected device.
It collects provenance only and never promotes device/owner/release verification.
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
EXPECTED_CANDIDATE_SCHEMA = 1
EXPECTED_CANDIDATE_TYPE = "local-windows-licensed-unity"
EXPECTED_CANDIDATE_VERDICT = "READY_FOR_PHYSICAL_DEVICE_EVIDENCE"
EVIDENCE_SCHEMA = "afareet-uper006-physical-device-evidence-v2"
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
    normalized = (value or "").strip().lower()
    if len(normalized) != 40 or any(ch not in "0123456789abcdef" for ch in normalized):
        raise EvidenceError("Git SHA must be the exact 40-character hexadecimal commit SHA")
    return normalized


def validate_sha256(value: str, *, label: str) -> str:
    normalized = (value or "").strip().lower()
    if len(normalized) != 64 or any(ch not in "0123456789abcdef" for ch in normalized):
        raise EvidenceError(f"{label} must be the exact 64-character hexadecimal SHA-256")
    return normalized


def require_text(payload: dict[str, Any], key: str, *, label: str) -> str:
    value = payload.get(key)
    if not isinstance(value, str) or not value.strip():
        raise EvidenceError(f"{label}.{key} must be non-blank text")
    return value.strip()


def require_number(payload: dict[str, Any], key: str, *, label: str, minimum: float = 0.0) -> float:
    value = payload.get(key)
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise EvidenceError(f"{label}.{key} must be numeric")
    number = float(value)
    if not math.isfinite(number) or number < minimum:
        raise EvidenceError(f"{label}.{key} must be finite and >= {minimum}")
    return number


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


def validate_runtime_report(report: dict[str, Any], *, minimum_samples: int = 300) -> dict[str, Any]:
    if report.get("schemaVersion") != EXPECTED_RUNTIME_SCHEMA:
        raise EvidenceError(f"runtime report schemaVersion must be {EXPECTED_RUNTIME_SCHEMA}")
    if report.get("evidenceId") != EXPECTED_EVIDENCE_ID:
        raise EvidenceError(f"runtime report evidenceId must be {EXPECTED_EVIDENCE_ID!r}")

    samples = report.get("samples")
    if isinstance(samples, bool) or not isinstance(samples, int) or samples < minimum_samples:
        raise EvidenceError(f"runtime report samples must be an integer >= {minimum_samples}")
    timing_samples = report.get("validFrameTimingSamples")
    if isinstance(timing_samples, bool) or not isinstance(timing_samples, int):
        raise EvidenceError("runtime report validFrameTimingSamples must be an integer")
    if timing_samples < 0 or timing_samples > samples:
        raise EvidenceError("runtime report validFrameTimingSamples must be within [0, samples]")

    for key in ("avgFps", "avgFrameMs", "p95FrameMs", "worstFrameMs", "avgCpuMs", "avgGpuMs", "peakReservedMb"):
        require_number(report, key, label="runtimeReport")
    if float(report["p95FrameMs"]) > float(report["worstFrameMs"]) + 0.0001:
        raise EvidenceError("runtime report p95FrameMs cannot exceed worstFrameMs")

    for key in (
        "capturedUtc", "deviceModel", "graphicsDeviceName", "operatingSystem", "processorType",
        "platform", "unityVersion", "appVersion", "qualityLevel",
    ):
        require_text(report, key, label="runtimeReport")
    for key in ("graphicsMemoryMb", "systemMemoryMb", "processorCount", "screenWidth", "screenHeight"):
        require_number(report, key, label="runtimeReport")
    return report


def validate_candidate_manifest(
    manifest: dict[str, Any], *, package: str, local_apk_path: Path, local_apk_sha256: str
) -> tuple[str, str]:
    if manifest.get("schemaVersion") != EXPECTED_CANDIDATE_SCHEMA:
        raise EvidenceError(f"candidate manifest schemaVersion must be {EXPECTED_CANDIDATE_SCHEMA}")
    if manifest.get("candidateType") != EXPECTED_CANDIDATE_TYPE:
        raise EvidenceError(f"candidate manifest candidateType must be {EXPECTED_CANDIDATE_TYPE!r}")
    git_sha = validate_git_sha(require_text(manifest, "gitSha", label="candidateManifest"))
    unity_version = require_text(manifest, "unityVersion", label="candidateManifest")
    if require_text(manifest, "packageId", label="candidateManifest") != package:
        raise EvidenceError("candidate manifest packageId does not match requested package")
    if manifest.get("releaseEvidenceEligible") is not True:
        raise EvidenceError("candidate manifest releaseEvidenceEligible must be JSON boolean true")
    if manifest.get("readyForDeviceEvidence") is not True:
        raise EvidenceError("candidate manifest readyForDeviceEvidence must be JSON boolean true")
    if manifest.get("verified") is not False:
        raise EvidenceError("candidate manifest verified must remain JSON boolean false")
    if manifest.get("verdict") != EXPECTED_CANDIDATE_VERDICT:
        raise EvidenceError(f"candidate manifest verdict must be {EXPECTED_CANDIDATE_VERDICT!r}")

    apk = manifest.get("apk")
    if not isinstance(apk, dict):
        raise EvidenceError("candidate manifest apk must be a JSON object")
    declared_hash = validate_sha256(require_text(apk, "sha256", label="candidateManifest.apk"), label="candidateManifest.apk.sha256")
    if declared_hash != local_apk_sha256:
        raise EvidenceError(f"candidate APK hash mismatch: manifest={declared_hash} local={local_apk_sha256}")
    file_name = require_text(apk, "fileName", label="candidateManifest.apk")
    if file_name != local_apk_path.name:
        raise EvidenceError(f"candidate APK filename mismatch: manifest={file_name!r} local={local_apk_path.name!r}")
    size = apk.get("sizeBytes")
    if isinstance(size, bool) or not isinstance(size, int) or size <= 0:
        raise EvidenceError("candidateManifest.apk.sizeBytes must be a positive integer")
    if local_apk_path.stat().st_size != size:
        raise EvidenceError("candidate APK size does not match candidate manifest")
    return git_sha, unity_version


def run_adb(adb: str, serial: str | None, args: list[str]) -> subprocess.CompletedProcess[str]:
    command = [adb]
    if serial:
        command.extend(["-s", serial])
    command.extend(args)
    completed = subprocess.run(command, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False)
    if completed.returncode != 0:
        raise EvidenceError(
            f"ADB command failed ({completed.returncode}): {' '.join(command)}\n"
            f"stdout={completed.stdout.strip()}\nstderr={completed.stderr.strip()}"
        )
    return completed


def run_adb_bytes(adb: str, serial: str, args: list[str]) -> subprocess.CompletedProcess[bytes]:
    command = [adb, "-s", serial, *args]
    completed = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False)
    if completed.returncode != 0:
        stderr = completed.stderr.decode("utf-8", errors="replace").strip()
        raise EvidenceError(f"ADB binary command failed ({completed.returncode}): {' '.join(command)}\nstderr={stderr}")
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
        raise EvidenceError(f"exactly one connected ADB device is required when --serial is omitted; found {len(devices)}")
    return devices[0]


def parse_installed_apk_path(pm_path_output: str, *, package: str) -> str:
    paths: list[str] = []
    for raw in pm_path_output.splitlines():
        line = raw.strip()
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
        raise EvidenceError(f"package {package!r} is installed as {len(paths)} split APKs; exact standalone APK evidence requires one APK")
    if not paths[0].lower().endswith(".apk"):
        raise EvidenceError(f"installed package path is not an APK: {paths[0]!r}")
    return paths[0]


def resolve_installed_apk_path(adb: str, serial: str, package: str) -> str:
    return parse_installed_apk_path(run_adb(adb, serial, ["shell", "pm", "path", package]).stdout, package=package)


def hash_installed_apk(adb: str, serial: str, installed_apk_path: str) -> str:
    if not installed_apk_path.strip():
        raise EvidenceError("installed APK path must be non-blank")
    return sha256_bytes(run_adb_bytes(adb, serial, ["exec-out", "cat", installed_apk_path]).stdout)


def pull_runtime_report(adb: str, serial: str, package: str) -> dict[str, Any]:
    payload = run_adb(adb, serial, ["shell", "run-as", package, "cat", f"files/{RUNTIME_REPORT_FILE}"]).stdout.strip()
    if not payload:
        raise EvidenceError(f"runtime report {RUNTIME_REPORT_FILE!r} was empty")
    try:
        report = json.loads(payload)
    except json.JSONDecodeError as exc:
        raise EvidenceError(f"runtime report is not valid JSON: {exc}") from exc
    if not isinstance(report, dict):
        raise EvidenceError("runtime report must be a JSON object")
    return report


def build_envelope(
    *, report: dict[str, Any], git_sha: str, apk_path: Path, apk_sha256: str, serial: str,
    package: str, installed_apk_path: str, installed_apk_sha256: str,
    candidate_manifest_path: Path | None, candidate_manifest_sha256: str | None,
    binding_mode: str,
) -> dict[str, Any]:
    return {
        "schema": EVIDENCE_SCHEMA,
        "evidenceId": EXPECTED_EVIDENCE_ID,
        "verdict": COLLECTION_VERDICT,
        "collectedUtc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "candidateBinding": {
            "mode": binding_mode,
            "manifestPath": str(candidate_manifest_path) if candidate_manifest_path else None,
            "manifestSha256": candidate_manifest_sha256,
            "gitSha": git_sha,
        },
        "candidateArtifact": {
            "packageId": package,
            "localApkPath": str(apk_path),
            "localApkSha256": apk_sha256,
            "installedApkPath": installed_apk_path,
            "installedApkSha256": installed_apk_sha256,
            "installedMatchesLocal": installed_apk_sha256 == apk_sha256,
        },
        "device": {
            "adbSerial": serial,
            "reportedModel": report.get("deviceModel"),
            "reportedGpu": report.get("graphicsDeviceName"),
            "reportedOs": report.get("operatingSystem"),
        },
        "runtimeReport": report,
        "acceptance": {
            "physicalDeviceVerified": False,
            "performanceTargetAccepted": False,
            "ownerApproval": False,
            "upER006Verified": False,
        },
        "verificationBoundary": (
            "Exact candidate provenance collection only. Matching manifest/local/installed APK bytes do not by themselves "
            "satisfy physical-device acceptance, performance acceptance, owner approval, UPER-006 verification, "
            "Last Verified APK, or publication approval."
        ),
    }


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apk", type=Path, required=True, help="Exact APK installed on the target device")
    binding = parser.add_mutually_exclusive_group(required=True)
    binding.add_argument("--candidate-manifest", type=Path, help="Licensed local-candidate-manifest.json")
    binding.add_argument("--git-sha", help="Legacy diagnostic binding only; exact 40-character Git SHA")
    parser.add_argument("--serial", help="ADB serial; omit only when exactly one device is connected")
    parser.add_argument("--package", default=DEFAULT_PACKAGE)
    parser.add_argument("--adb", default="adb")
    parser.add_argument("--output", type=Path, default=Path("uper006-performance-evidence.json"))
    parser.add_argument("--expected-apk-sha256")
    parser.add_argument("--minimum-samples", type=int, default=300)
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    try:
        apk_path = args.apk.resolve()
        if not apk_path.is_file() or apk_path.suffix.lower() != ".apk":
            raise EvidenceError(f"--apk must point to an existing .apk file: {apk_path}")
        if args.minimum_samples <= 0:
            raise EvidenceError("--minimum-samples must be positive")

        apk_sha256 = sha256_file(apk_path)
        if args.expected_apk_sha256:
            expected = validate_sha256(args.expected_apk_sha256, label="--expected-apk-sha256")
            if expected != apk_sha256:
                raise EvidenceError(f"APK SHA-256 mismatch: expected={expected} actual={apk_sha256}")

        manifest_path: Path | None = None
        manifest_hash: str | None = None
        expected_unity: str | None = None
        if args.candidate_manifest:
            manifest_path = args.candidate_manifest.resolve()
            manifest = load_json_object(manifest_path, label="candidate manifest")
            manifest_hash = sha256_file(manifest_path)
            git_sha, expected_unity = validate_candidate_manifest(
                manifest, package=args.package, local_apk_path=apk_path, local_apk_sha256=apk_sha256
            )
            binding_mode = MANIFEST_BINDING
        else:
            git_sha = validate_git_sha(args.git_sha)
            binding_mode = LEGACY_BINDING

        serial = resolve_serial(args.adb, args.serial)
        installed_path = resolve_installed_apk_path(args.adb, serial, args.package)
        installed_hash = hash_installed_apk(args.adb, serial, installed_path)
        if installed_hash != apk_sha256:
            raise EvidenceError(f"installed APK SHA-256 mismatch: installed={installed_hash} local={apk_sha256}")

        report = validate_runtime_report(
            pull_runtime_report(args.adb, serial, args.package), minimum_samples=args.minimum_samples
        )
        if expected_unity is not None and report["unityVersion"] != expected_unity:
            raise EvidenceError(
                f"runtime Unity version does not match candidate manifest: report={report['unityVersion']!r} manifest={expected_unity!r}"
            )

        envelope = build_envelope(
            report=report, git_sha=git_sha, apk_path=apk_path, apk_sha256=apk_sha256,
            serial=serial, package=args.package, installed_apk_path=installed_path,
            installed_apk_sha256=installed_hash, candidate_manifest_path=manifest_path,
            candidate_manifest_sha256=manifest_hash, binding_mode=binding_mode,
        )
        output = args.output.resolve()
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(json.dumps(envelope, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(
            "AFAREET_UPER006_PERFORMANCE_EVIDENCE_COLLECTED "
            f"output={output} gitSha={git_sha} apkSha256={apk_sha256} serial={serial} "
            f"binding={binding_mode} installedApkMatch=true verdict={COLLECTION_VERDICT}"
        )
        return 0
    except EvidenceError as exc:
        print(f"AFAREET_UPER006_PERFORMANCE_EVIDENCE_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
