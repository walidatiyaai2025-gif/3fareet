#!/usr/bin/env python3
"""Deterministic ADB evidence collector for 3Fareet Android QA.

This tool collects evidence. It deliberately does not decide gameplay feel,
race correctness, visual quality, or release readiness.
"""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Any, Iterable

PACKAGE_ID = "com.fiftysolutions.afareetunity3d"
SESSION_FILE = "session.json"
INDEX_FILE = "evidence-index.json"
CRASH_PATTERNS = (
    re.compile(r"FATAL EXCEPTION", re.IGNORECASE),
    re.compile(rf"ANR in\s+{re.escape(PACKAGE_ID)}", re.IGNORECASE),
    re.compile(r"signal\s+[0-9]+\s+\(SIG(?:SEGV|ABRT|BUS|ILL)\)", re.IGNORECASE),
    re.compile(r"Unity.*(?:crash|fatal)", re.IGNORECASE),
)


def utc_now() -> str:
    return dt.datetime.now(dt.timezone.utc).replace(microsecond=0).isoformat()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def sanitize_label(value: str) -> str:
    cleaned = re.sub(r"[^A-Za-z0-9._-]+", "-", value.strip())
    cleaned = cleaned.strip("-._")
    if not cleaned:
        raise ValueError("Checkpoint label must contain at least one safe character.")
    return cleaned[:64]


def run(command: list[str], *, text: bool = True, check: bool = True) -> subprocess.CompletedProcess[Any]:
    return subprocess.run(
        command,
        check=check,
        capture_output=True,
        text=text,
    )


def require_executable(name: str) -> str:
    resolved = shutil.which(name)
    if not resolved:
        raise RuntimeError(f"Required executable is not available on PATH: {name}")
    return resolved


def adb(serial: str, *args: str, text: bool = True, check: bool = True) -> subprocess.CompletedProcess[Any]:
    return run(["adb", "-s", serial, *args], text=text, check=check)


def parse_adb_devices(output: str) -> list[dict[str, str]]:
    devices: list[dict[str, str]] = []
    for raw in output.splitlines():
        line = raw.strip()
        if not line or line.startswith("List of devices"):
            continue

        parts = line.split()
        if len(parts) < 2:
            continue

        serial = parts[0]
        state = parts[1]
        metadata: dict[str, str] = {"serial": serial, "state": state}
        for token in parts[2:]:
            if ":" in token:
                key, value = token.split(":", 1)
                metadata[key] = value
        devices.append(metadata)
    return devices


def list_devices() -> list[dict[str, str]]:
    result = run(["adb", "devices", "-l"])
    return parse_adb_devices(result.stdout)


def select_device(requested_serial: str | None) -> dict[str, str]:
    available = [item for item in list_devices() if item.get("state") == "device"]
    if requested_serial:
        for item in available:
            if item["serial"] == requested_serial:
                return item
        raise RuntimeError(f"Requested ADB device is not connected/authorized: {requested_serial}")
    if len(available) != 1:
        serials = ", ".join(item["serial"] for item in available) or "none"
        raise RuntimeError(
            "Exactly one authorized Android device is required when --serial is omitted; "
            f"found {len(available)} ({serials})."
        )
    return available[0]


def shell_text(serial: str, *args: str, check: bool = True) -> str:
    return adb(serial, "shell", *args, check=check).stdout.strip()


def getprop(serial: str, name: str) -> str:
    return shell_text(serial, "getprop", name, check=False)


def is_emulator(serial: str) -> bool:
    if serial.startswith("emulator-"):
        return True
    values = {
        getprop(serial, "ro.kernel.qemu"),
        getprop(serial, "ro.boot.qemu"),
        getprop(serial, "ro.hardware"),
    }
    normalized = {value.strip().lower() for value in values}
    return "1" in normalized or "ranchu" in normalized or "goldfish" in normalized


def collect_device_metadata(serial: str, adb_entry: dict[str, str]) -> dict[str, Any]:
    return {
        "serial": serial,
        "serialSha256": hashlib.sha256(serial.encode("utf-8")).hexdigest(),
        "adbMetadata": adb_entry,
        "manufacturer": getprop(serial, "ro.product.manufacturer"),
        "model": getprop(serial, "ro.product.model"),
        "device": getprop(serial, "ro.product.device"),
        "androidRelease": getprop(serial, "ro.build.version.release"),
        "apiLevel": getprop(serial, "ro.build.version.sdk"),
        "primaryAbi": getprop(serial, "ro.product.cpu.abi"),
        "supportedAbis": getprop(serial, "ro.product.cpu.abilist"),
        "hardware": getprop(serial, "ro.hardware"),
        "isEmulator": is_emulator(serial),
        "displaySize": shell_text(serial, "wm", "size", check=False),
        "displayDensity": shell_text(serial, "wm", "density", check=False),
    }


def scan_logcat(text: str) -> list[str]:
    findings: list[str] = []
    for line in text.splitlines():
        if any(pattern.search(line) for pattern in CRASH_PATTERNS):
            findings.append(line.strip())
    return findings[:100]


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def read_session(directory: Path) -> dict[str, Any]:
    session_path = directory / SESSION_FILE
    if not session_path.is_file():
        raise RuntimeError(f"Session file is missing: {session_path}")
    return json.loads(session_path.read_text(encoding="utf-8"))


def package_summary(serial: str) -> str:
    return shell_text(serial, "dumpsys", "package", PACKAGE_ID, check=False)


def command_prepare(args: argparse.Namespace) -> int:
    require_executable("adb")
    apk = Path(args.apk).expanduser().resolve()
    if not apk.is_file() or apk.stat().st_size <= 0:
        raise RuntimeError(f"APK is missing or empty: {apk}")

    device_entry = select_device(args.serial)
    serial = device_entry["serial"]
    device = collect_device_metadata(serial, device_entry)
    if device["isEmulator"] and not args.allow_emulator:
        raise RuntimeError(
            "A physical Android device is required for P1 device evidence. "
            "Use --allow-emulator only for harness debugging; emulator evidence cannot satisfy the device gates."
        )

    output = Path(args.output).expanduser().resolve()
    output.mkdir(parents=True, exist_ok=True)
    if (output / SESSION_FILE).exists() and not args.force:
        raise RuntimeError(f"Evidence session already exists at {output}; use --force to replace it.")

    apk_hash = sha256_file(apk)
    adb(serial, "install", "-r", "-t", str(apk))
    installed = package_summary(serial)
    if PACKAGE_ID not in installed:
        raise RuntimeError(f"Installed package could not be confirmed: {PACKAGE_ID}")

    adb(serial, "logcat", "-c", check=False)
    shell_text(serial, "am", "force-stop", PACKAGE_ID, check=False)
    launch = shell_text(
        serial,
        "monkey",
        "-p",
        PACKAGE_ID,
        "-c",
        "android.intent.category.LAUNCHER",
        "1",
        check=False,
    )

    session = {
        "schemaVersion": 1,
        "createdAtUtc": utc_now(),
        "state": "PREPARED",
        "verdict": "MANUAL_REVIEW_REQUIRED",
        "packageId": PACKAGE_ID,
        "apk": {
            "path": str(apk),
            "fileName": apk.name,
            "sizeBytes": apk.stat().st_size,
            "sha256": apk_hash,
        },
        "device": device,
        "launchOutput": launch,
        "checkpointCount": 0,
        "notes": [
            "This harness collects evidence only.",
            "Physical driving feel, race correctness and visual quality require human review.",
        ],
    }
    write_json(output / SESSION_FILE, session)
    (output / "package-dump.txt").write_text(installed, encoding="utf-8", errors="replace")
    print(f"AFAREET_DEVICE_EVIDENCE_PREPARED session={output} apkSha256={apk_hash} serial={serial}")
    return 0


def capture_text(serial: str, target: Path, *shell_args: str) -> None:
    content = shell_text(serial, *shell_args, check=False)
    target.write_text(content + ("\n" if content else ""), encoding="utf-8", errors="replace")


def command_capture(args: argparse.Namespace) -> int:
    require_executable("adb")
    session_dir = Path(args.session).expanduser().resolve()
    session = read_session(session_dir)
    serial = session["device"]["serial"]
    label = sanitize_label(args.label)
    checkpoint_dir = session_dir / "checkpoints" / label
    if checkpoint_dir.exists() and not args.force:
        raise RuntimeError(f"Checkpoint already exists: {checkpoint_dir}; use --force to replace it.")
    checkpoint_dir.mkdir(parents=True, exist_ok=True)

    current = select_device(serial)
    if current["serial"] != serial:
        raise RuntimeError("Connected device does not match the evidence session.")

    screenshot = adb(serial, "exec-out", "screencap", "-p", text=False).stdout
    if not screenshot.startswith(b"\x89PNG\r\n\x1a\n"):
        raise RuntimeError("ADB screenshot did not return a valid PNG payload.")
    (checkpoint_dir / "screen.png").write_bytes(screenshot)

    logcat = adb(serial, "logcat", "-d", "-v", "threadtime", check=False).stdout
    (checkpoint_dir / "logcat.txt").write_text(logcat, encoding="utf-8", errors="replace")
    capture_text(serial, checkpoint_dir / "meminfo.txt", "dumpsys", "meminfo", PACKAGE_ID)
    capture_text(serial, checkpoint_dir / "gfxinfo.txt", "dumpsys", "gfxinfo", PACKAGE_ID)
    capture_text(serial, checkpoint_dir / "thermalservice.txt", "dumpsys", "thermalservice")
    capture_text(serial, checkpoint_dir / "battery.txt", "dumpsys", "battery")
    capture_text(serial, checkpoint_dir / "activity.txt", "dumpsys", "activity", "activities", PACKAGE_ID)

    findings = scan_logcat(logcat)
    metadata = {
        "schemaVersion": 1,
        "label": label,
        "capturedAtUtc": utc_now(),
        "apkSha256": session["apk"]["sha256"],
        "deviceSerialSha256": session["device"]["serialSha256"],
        "automatedRedFlags": findings,
        "automatedRedFlagCount": len(findings),
        "manualReviewRequired": True,
        "files": [
            "screen.png",
            "logcat.txt",
            "meminfo.txt",
            "gfxinfo.txt",
            "thermalservice.txt",
            "battery.txt",
            "activity.txt",
        ],
    }
    write_json(checkpoint_dir / "checkpoint.json", metadata)

    session["checkpointCount"] = len(list((session_dir / "checkpoints").glob("*/checkpoint.json")))
    session["lastCapturedAtUtc"] = metadata["capturedAtUtc"]
    write_json(session_dir / SESSION_FILE, session)
    print(
        f"AFAREET_DEVICE_EVIDENCE_CAPTURED label={label} "
        f"redFlags={len(findings)} path={checkpoint_dir}"
    )
    return 2 if findings else 0


def load_checkpoint_metadata(session_dir: Path) -> list[dict[str, Any]]:
    root = session_dir / "checkpoints"
    if not root.exists():
        return []
    records: list[dict[str, Any]] = []
    for path in sorted(root.glob("*/checkpoint.json")):
        records.append(json.loads(path.read_text(encoding="utf-8")))
    return records


def command_finish(args: argparse.Namespace) -> int:
    session_dir = Path(args.session).expanduser().resolve()
    session = read_session(session_dir)
    checkpoints = load_checkpoint_metadata(session_dir)
    red_flags: list[dict[str, Any]] = []
    for checkpoint in checkpoints:
        for finding in checkpoint.get("automatedRedFlags", []):
            red_flags.append({"checkpoint": checkpoint["label"], "finding": finding})

    index = {
        "schemaVersion": 1,
        "generatedAtUtc": utc_now(),
        "state": "EVIDENCE_COLLECTED" if checkpoints else "NO_CHECKPOINTS",
        "verdict": "MANUAL_REVIEW_REQUIRED",
        "packageId": session["packageId"],
        "apkSha256": session["apk"]["sha256"],
        "deviceSerialSha256": session["device"]["serialSha256"],
        "device": {
            "manufacturer": session["device"].get("manufacturer", ""),
            "model": session["device"].get("model", ""),
            "androidRelease": session["device"].get("androidRelease", ""),
            "apiLevel": session["device"].get("apiLevel", ""),
            "primaryAbi": session["device"].get("primaryAbi", ""),
            "isEmulator": session["device"].get("isEmulator", False),
        },
        "checkpointCount": len(checkpoints),
        "checkpoints": [item["label"] for item in checkpoints],
        "automatedRedFlagCount": len(red_flags),
        "automatedRedFlags": red_flags,
        "manualReviewChecklist": [
            "UVEH-012: evaluate steering/brake/reverse/drift/nitro/reset feel on a physical device.",
            "URAC-012: complete ordered lap, Results, restart and a second countdown on device.",
            "UPER-006: review crash/ANR, memory, frame timing, temperature and device-specific behavior.",
            "UPER-009: review captured screenshots for Hero/Cairo/HUD/readability/SafeArea/Arabic-English visual gates.",
            "UPER-010: do not publish until exact APK SHA and all required manual gates are approved.",
        ],
    }
    write_json(session_dir / INDEX_FILE, index)
    session["state"] = index["state"]
    session["verdict"] = index["verdict"]
    session["finishedAtUtc"] = index["generatedAtUtc"]
    session["checkpointCount"] = len(checkpoints)
    session["automatedRedFlagCount"] = len(red_flags)
    write_json(session_dir / SESSION_FILE, session)
    print(
        f"AFAREET_DEVICE_EVIDENCE_FINISHED checkpoints={len(checkpoints)} "
        f"redFlags={len(red_flags)} verdict=MANUAL_REVIEW_REQUIRED index={session_dir / INDEX_FILE}"
    )
    return 2 if red_flags else 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Collect deterministic 3Fareet Android device QA evidence.")
    sub = parser.add_subparsers(dest="command", required=True)

    prepare = sub.add_parser("prepare", help="Install/launch an APK and create a pinned device evidence session.")
    prepare.add_argument("--apk", required=True, help="Path to the exact APK under test.")
    prepare.add_argument("--output", required=True, help="Output directory for the evidence session.")
    prepare.add_argument("--serial", help="ADB serial. Required when more than one authorized device is connected.")
    prepare.add_argument("--allow-emulator", action="store_true", help="Allow emulator use for harness debugging only.")
    prepare.add_argument("--force", action="store_true", help="Replace an existing session in --output.")
    prepare.set_defaults(func=command_prepare)

    capture = sub.add_parser("capture", help="Capture screenshot/log/performance evidence at a named QA checkpoint.")
    capture.add_argument("--session", required=True, help="Evidence session directory created by prepare.")
    capture.add_argument("--label", required=True, help="Stable checkpoint label, e.g. start, race-2min, results.")
    capture.add_argument("--force", action="store_true", help="Replace an existing checkpoint with this label.")
    capture.set_defaults(func=command_capture)

    finish = sub.add_parser("finish", help="Aggregate checkpoint evidence without auto-approving manual gates.")
    finish.add_argument("--session", required=True, help="Evidence session directory.")
    finish.set_defaults(func=command_finish)
    return parser


def main(argv: Iterable[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(list(argv) if argv is not None else None)
    try:
        return int(args.func(args))
    except (RuntimeError, ValueError, subprocess.CalledProcessError) as exc:
        print(f"AFAREET_DEVICE_EVIDENCE_ERROR {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
