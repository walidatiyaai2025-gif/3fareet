#!/usr/bin/env python3
"""Summarize UPER-006 Android smoke evidence without self-asserting release verification.

The collector already stores raw meminfo/gfxinfo/thermal/battery data per checkpoint.
This tool turns the Android-observable subset into deterministic machine-readable metrics
and applies the numeric UPER-001 budgets that ADB can evaluate. Unity main/render/GPU
profiler timings remain manual/profiler evidence and are never invented here.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path
from typing import Any

REQUIRED_LABELS = ("smoke-cold-start", "smoke-warm-race", "smoke-after-restarts")
TIER_BUDGETS = {
    "low": {"p95Ms": 33.3, "p99Ms": 40.0, "steadyPssMiB": 650.0, "restartGrowthPct": 5.0},
    "mid": {"p95Ms": 16.7, "p99Ms": 22.0, "steadyPssMiB": 900.0, "restartGrowthPct": 5.0},
    "high": {"p95Ms": 16.7, "p99Ms": 20.0, "steadyPssMiB": 1100.0, "restartGrowthPct": 5.0},
}
SEVERE_THERMAL_STATUS = 3
SHA256_PATTERN = re.compile(r"^[0-9a-f]{64}$", flags=re.IGNORECASE)


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="replace") if path.is_file() else ""


def read_json(path: Path) -> dict[str, Any]:
    if not path.is_file():
        raise RuntimeError(f"required file missing: {path}")
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise RuntimeError(f"JSON root must be an object: {path}")
    return value


def is_sha256(value: Any) -> bool:
    return bool(SHA256_PATTERN.fullmatch(str(value or "").strip()))


def parse_total_pss_kib(text: str) -> int | None:
    patterns = (
        r"TOTAL\s+PSS:\s*([0-9,]+)",
        r"^\s*TOTAL\s+([0-9,]+)\s+",
    )
    for pattern in patterns:
        match = re.search(pattern, text, flags=re.IGNORECASE | re.MULTILINE)
        if match:
            return int(match.group(1).replace(",", ""))
    return None


def parse_percentile_ms(text: str, percentile: int) -> float | None:
    match = re.search(rf"{percentile}th percentile:\s*([0-9.]+)ms", text, flags=re.IGNORECASE)
    return float(match.group(1)) if match else None


def parse_janky_percent(text: str) -> float | None:
    match = re.search(r"Janky frames:\s*[0-9,]+\s*\(([0-9.]+)%\)", text, flags=re.IGNORECASE)
    return float(match.group(1)) if match else None


def parse_thermal_status(text: str) -> int | None:
    matches = re.findall(r"(?:Thermal\s+Status|mStatus|Status)\s*[:=]\s*([0-6])\b", text, flags=re.IGNORECASE)
    return max((int(value) for value in matches), default=None)


def parse_battery(text: str) -> dict[str, Any]:
    def field(name: str) -> str:
        match = re.search(rf"^\s*{re.escape(name)}:\s*(.+)$", text, flags=re.IGNORECASE | re.MULTILINE)
        return match.group(1).strip() if match else ""

    return {
        "level": field("level"),
        "status": field("status"),
        "usbPowered": field("USB powered"),
        "acPowered": field("AC powered"),
        "wirelessPowered": field("Wireless powered"),
    }


def summarize_checkpoint(checkpoint_dir: Path) -> dict[str, Any]:
    metadata = read_json(checkpoint_dir / "checkpoint.json")
    pss_kib = parse_total_pss_kib(read_text(checkpoint_dir / "meminfo.txt"))
    gfx = read_text(checkpoint_dir / "gfxinfo.txt")
    thermal = read_text(checkpoint_dir / "thermalservice.txt")

    red_flag_present = "automatedRedFlagCount" in metadata
    try:
        red_flag_count = int(metadata.get("automatedRedFlagCount", 0) or 0)
    except (TypeError, ValueError):
        red_flag_count = -1

    return {
        "label": str(metadata.get("label") or ""),
        "labelPresent": "label" in metadata and bool(str(metadata.get("label") or "").strip()),
        "apkSha256": str(metadata.get("apkSha256") or "").strip(),
        "deviceSerialSha256": str(metadata.get("deviceSerialSha256") or "").strip(),
        "automatedRedFlagCount": red_flag_count,
        "automatedRedFlagCountPresent": red_flag_present,
        "pssMiB": None if pss_kib is None else round(pss_kib / 1024.0, 3),
        "frameP95Ms": parse_percentile_ms(gfx, 95),
        "frameP99Ms": parse_percentile_ms(gfx, 99),
        "jankyFramePercent": parse_janky_percent(gfx),
        "thermalStatusMax": parse_thermal_status(thermal),
        "battery": parse_battery(read_text(checkpoint_dir / "battery.txt")),
    }


def analyze(session_dir: Path, tier: str) -> dict[str, Any]:
    tier_key = tier.lower()
    if tier_key not in TIER_BUDGETS:
        raise ValueError(f"unsupported tier: {tier}")
    budget = TIER_BUDGETS[tier_key]
    session = read_json(session_dir / "session.json")
    checkpoints_root = session_dir / "checkpoints"
    checkpoints: dict[str, dict[str, Any]] = {}
    blockers: list[str] = []

    apk_sha = str(session.get("apk", {}).get("sha256") or "").strip() if isinstance(session.get("apk"), dict) else ""
    device_sha = str(session.get("device", {}).get("serialSha256") or "").strip() if isinstance(session.get("device"), dict) else ""
    if not is_sha256(apk_sha):
        blockers.append("session: missing or invalid APK SHA-256 fingerprint")
    if not is_sha256(device_sha):
        blockers.append("session: missing or invalid device serial SHA-256 fingerprint")

    for label in REQUIRED_LABELS:
        checkpoint_dir = checkpoints_root / label
        if not checkpoint_dir.is_dir():
            blockers.append(f"missing required smoke checkpoint: {label}")
            continue

        record = summarize_checkpoint(checkpoint_dir)
        checkpoints[label] = record

        if not record["labelPresent"]:
            blockers.append(f"{label}: checkpoint metadata label is missing")
        elif record["label"] != label:
            blockers.append(f"{label}: checkpoint metadata label mismatch ({record['label']})")

        if not is_sha256(record["apkSha256"]):
            blockers.append(f"{label}: missing or invalid checkpoint APK SHA-256 fingerprint")
        elif is_sha256(apk_sha) and record["apkSha256"].lower() != apk_sha.lower():
            blockers.append(f"{label}: checkpoint APK SHA does not match session")

        if not is_sha256(record["deviceSerialSha256"]):
            blockers.append(f"{label}: missing or invalid checkpoint device SHA-256 fingerprint")
        elif is_sha256(device_sha) and record["deviceSerialSha256"].lower() != device_sha.lower():
            blockers.append(f"{label}: checkpoint device does not match session")

        if not record["automatedRedFlagCountPresent"] or record["automatedRedFlagCount"] < 0:
            blockers.append(f"{label}: automated red-flag count is missing or invalid")
        elif record["automatedRedFlagCount"] > 0:
            blockers.append(f"{label}: crash/ANR/native-fatal red flags present")

        if record["thermalStatusMax"] is not None and record["thermalStatusMax"] >= SEVERE_THERMAL_STATUS:
            blockers.append(f"{label}: Android thermal status reached SEVERE or worse ({record['thermalStatusMax']})")

    for label in ("smoke-warm-race", "smoke-after-restarts"):
        record = checkpoints.get(label)
        if not record:
            continue
        pss = record.get("pssMiB")
        if pss is None:
            blockers.append(f"{label}: process PSS could not be parsed")
        elif pss > budget["steadyPssMiB"]:
            blockers.append(f"{label}: PSS {pss:.1f} MiB exceeds {tier_key} steady budget {budget['steadyPssMiB']:.1f} MiB")
        for field, budget_name in (("frameP95Ms", "p95Ms"), ("frameP99Ms", "p99Ms")):
            value = record.get(field)
            if value is None:
                blockers.append(f"{label}: {field} could not be parsed from gfxinfo")
            elif value > budget[budget_name]:
                blockers.append(f"{label}: {field} {value:.1f} ms exceeds {tier_key} budget {budget[budget_name]:.1f} ms")

    warm = checkpoints.get("smoke-warm-race")
    restarted = checkpoints.get("smoke-after-restarts")
    restart_growth = None
    if warm and restarted and warm.get("pssMiB") is not None and restarted.get("pssMiB") is not None:
        baseline = float(warm["pssMiB"])
        if baseline > 0:
            restart_growth = round(((float(restarted["pssMiB"]) - baseline) / baseline) * 100.0, 3)
            if restart_growth > budget["restartGrowthPct"]:
                blockers.append(
                    f"restart PSS growth {restart_growth:.2f}% exceeds {budget['restartGrowthPct']:.1f}% budget"
                )
        else:
            blockers.append("smoke-warm-race: process PSS baseline must be greater than zero")
    elif warm or restarted:
        blockers.append("restart memory growth could not be calculated")

    return {
        "schemaVersion": 1,
        "taskId": "UPER-006",
        "tier": tier_key.upper(),
        "verified": False,
        "verdict": "BLOCKED" if blockers else "PASSABLE_FOR_MANUAL_REVIEW",
        "apkSha256": apk_sha,
        "deviceSerialSha256": device_sha,
        "budget": budget,
        "restartPssGrowthPercent": restart_growth,
        "checkpoints": checkpoints,
        "blockers": blockers,
        "manualEvidenceStillRequired": [
            "physical-device behavior review",
            "Unity main/render/GPU profiler timing where required by UPER-001",
            "sustained thermal/degradation review for the full performance gate",
        ],
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Analyze UPER-006 Android smoke checkpoint metrics")
    parser.add_argument("--session", required=True)
    parser.add_argument("--tier", required=True, choices=("low", "mid", "high"))
    parser.add_argument("--output", default=None)
    args = parser.parse_args()

    session_dir = Path(args.session).expanduser().resolve()
    result = analyze(session_dir, args.tier)
    output = Path(args.output).expanduser().resolve() if args.output else session_dir / "uper006-smoke-metrics.json"
    output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"AFAREET_UPER006_SMOKE_METRICS verdict={result['verdict']} blockers={len(result['blockers'])} output={output}")
    return 0 if not result["blockers"] else 2


if __name__ == "__main__":
    raise SystemExit(main())
