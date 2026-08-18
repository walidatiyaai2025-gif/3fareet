#!/usr/bin/env python3
"""Validate the exact Afareet King refinement handoff without promoting it to production art."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any

EXPECTED_SCHEMA = 1
EXPECTED_TASK = "UART-003"
EXPECTED_CLASSIFICATION = "REFINEMENT_CANDIDATE"
EXPECTED_ORIGIN = "EXTERNAL_USER_HANDOFF"
EXPECTED_BOUNDARY = "BYTE_IDENTITY_ONLY_LICENSED_UNITY_INSPECTION_REQUIRED"
EXPECTED_FBX_ROLE = "UNITY_REFINEMENT_INTAKE"
EXPECTED_GLB_ROLE = "INSPECTION_COMPANION"
EXPECTED_BLEND_ROLE = "DCC_SOURCE_COMPANION"
VERDICT = "REFINEMENT_HANDOFF_MATCH_NOT_PRODUCTION"
_SHA256_RE = re.compile(r"^[0-9a-f]{64}$")


class HandoffError(RuntimeError):
    pass


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise HandoffError(message)


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _load_json(path: Path, label: str) -> dict[str, Any]:
    _require(path.is_file(), f"{label} does not exist: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise HandoffError(f"{label} is not valid JSON: {exc}") from exc
    _require(isinstance(payload, dict), f"{label} root must be a JSON object")
    return payload


def _require_false(payload: dict[str, Any], key: str, label: str) -> None:
    _require(key in payload and payload[key] is False, f"{label}.{key} must be JSON false")


def _validate_file_record(record: Any, *, label: str, expected_role: str) -> dict[str, Any]:
    _require(isinstance(record, dict), f"receipt.files.{label} must be an object")
    file_name = record.get("fileName")
    sha256 = record.get("sha256")
    size = record.get("sizeBytes")
    role = record.get("role")
    _require(isinstance(file_name, str) and file_name.strip(), f"receipt.files.{label}.fileName must be non-blank")
    _require(
        isinstance(sha256, str) and _SHA256_RE.fullmatch(sha256) is not None,
        f"receipt.files.{label}.sha256 must be lowercase SHA-256",
    )
    _require(
        isinstance(size, int) and not isinstance(size, bool) and size > 0,
        f"receipt.files.{label}.sizeBytes must be a positive integer",
    )
    _require(role == expected_role, f"receipt.files.{label}.role must be {expected_role}")
    return record


def validate_receipt(receipt: dict[str, Any], manifest: dict[str, Any]) -> dict[str, dict[str, Any]]:
    _require(receipt.get("schemaVersion") == EXPECTED_SCHEMA, f"receipt.schemaVersion must be {EXPECTED_SCHEMA}")
    _require(receipt.get("task") == EXPECTED_TASK, f"receipt.task must be {EXPECTED_TASK}")
    _require(
        receipt.get("classification") == EXPECTED_CLASSIFICATION,
        f"receipt.classification must be {EXPECTED_CLASSIFICATION}",
    )
    _require(receipt.get("origin") == EXPECTED_ORIGIN, f"receipt.origin must be {EXPECTED_ORIGIN}")
    _require(
        receipt.get("inspectionBoundary") == EXPECTED_BOUNDARY,
        f"receipt.inspectionBoundary must be {EXPECTED_BOUNDARY}",
    )
    for key in ("productionGate", "visualAcceptance", "ownerApproval", "verified"):
        _require_false(receipt, key, "receipt")

    _require(
        manifest.get("schemaVersion") == EXPECTED_SCHEMA,
        f"refinement manifest schemaVersion must be {EXPECTED_SCHEMA}",
    )
    _require(
        manifest.get("classification") == EXPECTED_CLASSIFICATION,
        f"refinement manifest classification must be {EXPECTED_CLASSIFICATION}",
    )
    _require_false(manifest, "productionGate", "refinement manifest")
    _require_false(manifest, "visualAcceptance", "refinement manifest")

    files = receipt.get("files")
    _require(isinstance(files, dict), "receipt.files must be an object")
    fbx = _validate_file_record(files.get("fbx"), label="fbx", expected_role=EXPECTED_FBX_ROLE)
    glb = _validate_file_record(files.get("glb"), label="glb", expected_role=EXPECTED_GLB_ROLE)
    blend = _validate_file_record(files.get("blend"), label="blend", expected_role=EXPECTED_BLEND_ROLE)

    _require(
        fbx["fileName"] == manifest.get("sourceFileName"),
        "receipt FBX fileName must match hero_refinement_candidate_manifest.json",
    )
    _require(
        fbx["sha256"] == manifest.get("sha256"),
        "receipt FBX SHA-256 must match hero_refinement_candidate_manifest.json",
    )
    _require(
        fbx["sizeBytes"] == manifest.get("sizeBytes"),
        "receipt FBX sizeBytes must match hero_refinement_candidate_manifest.json",
    )

    names = {fbx["fileName"], glb["fileName"], blend["fileName"]}
    _require(len(names) == 3, "handoff file names must be unique")
    return {"fbx": fbx, "glb": glb, "blend": blend}


def verify_exact_file(path: Path, record: dict[str, Any], *, label: str) -> dict[str, Any]:
    _require(path.is_file(), f"{label} file does not exist: {path}")
    _require(
        path.name == record["fileName"],
        f"{label} file name mismatch: expected={record['fileName']} actual={path.name}",
    )
    actual_size = path.stat().st_size
    _require(
        actual_size == record["sizeBytes"],
        f"{label} size mismatch: expected={record['sizeBytes']} actual={actual_size}",
    )
    actual_sha = sha256_file(path)
    _require(
        actual_sha == record["sha256"],
        f"{label} SHA-256 mismatch: expected={record['sha256']} actual={actual_sha}",
    )
    return {
        "fileName": path.name,
        "sizeBytes": actual_size,
        "sha256": actual_sha,
        "byteIdentityMatch": True,
    }


def build_result(receipt: dict[str, Any], verified_files: dict[str, dict[str, Any]]) -> dict[str, Any]:
    return {
        "schemaVersion": 1,
        "task": EXPECTED_TASK,
        "classification": EXPECTED_CLASSIFICATION,
        "origin": EXPECTED_ORIGIN,
        "verdict": VERDICT,
        "handoffByteIdentityMatch": True,
        "files": verified_files,
        "productionGate": False,
        "visualAcceptance": False,
        "ownerApproval": False,
        "verified": False,
        "inspectionBoundary": receipt["inspectionBoundary"],
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fbx", type=Path, required=True, help="Exact AfareetKing_Hero.fbx handoff file")
    parser.add_argument("--glb", type=Path, required=True, help="Exact AfareetKing_Hero.glb inspection companion")
    parser.add_argument("--blend", type=Path, required=True, help="Exact AfareetKing_Hero.blend source companion")
    parser.add_argument(
        "--receipt",
        type=Path,
        default=Path("tools/android/hero_refinement_handoff_receipt.json"),
    )
    parser.add_argument(
        "--refinement-manifest",
        type=Path,
        default=Path("tools/android/hero_refinement_candidate_manifest.json"),
    )
    parser.add_argument("--output", type=Path, help="Optional JSON result; existing files are never overwritten")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(sys.argv[1:] if argv is None else argv)
    try:
        receipt = _load_json(args.receipt, "handoff receipt")
        manifest = _load_json(args.refinement_manifest, "refinement manifest")
        records = validate_receipt(receipt, manifest)
        verified_files = {
            "fbx": verify_exact_file(args.fbx, records["fbx"], label="FBX"),
            "glb": verify_exact_file(args.glb, records["glb"], label="GLB"),
            "blend": verify_exact_file(args.blend, records["blend"], label="BLEND"),
        }
        result = build_result(receipt, verified_files)
        if args.output:
            output = args.output.resolve()
            _require(not output.exists(), f"refusing to overwrite existing result: {output}")
            output.parent.mkdir(parents=True, exist_ok=True)
            output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(
            "AFAREET_HERO_REFINEMENT_HANDOFF_OK "
            f"verdict={VERDICT} fbxSha256={verified_files['fbx']['sha256']} "
            "productionGate=false verified=false"
        )
        return 0
    except (HandoffError, OSError, ValueError) as exc:
        print(f"AFAREET_HERO_REFINEMENT_HANDOFF_BLOCKED error={exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
