#!/usr/bin/env python3
"""Fail-closed consistency check for Unity Packages/manifest.json vs packages-lock.json."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


KNOWN_DIRECT_DEPENDENCIES: dict[str, dict[str, str]] = {
    "com.unity.inputsystem": {"com.unity.modules.uielements": "1.0.0"},
    "com.unity.ugui": {
        "com.unity.modules.ui": "1.0.0",
        "com.unity.modules.imgui": "1.0.0",
        "com.unity.modules.audio": "1.0.0",
        "com.unity.modules.physics2d": "1.0.0",
        "com.unity.modules.physics": "1.0.0",
    },
    "com.unity.modules.vehicles": {"com.unity.modules.physics": "1.0.0"},
}


class PackageLockError(ValueError):
    pass


def _load_json(path: Path) -> dict[str, Any]:
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as exc:
        raise PackageLockError(f"missing file: {path}") from exc
    except json.JSONDecodeError as exc:
        raise PackageLockError(f"invalid JSON in {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise PackageLockError(f"expected JSON object in {path}")
    return payload


def verify(manifest_path: Path, lock_path: Path) -> list[str]:
    manifest = _load_json(manifest_path)
    lock = _load_json(lock_path)

    manifest_deps = manifest.get("dependencies")
    lock_deps = lock.get("dependencies")
    if not isinstance(manifest_deps, dict) or not manifest_deps:
        raise PackageLockError("manifest dependencies must be a non-empty object")
    if not isinstance(lock_deps, dict) or not lock_deps:
        raise PackageLockError("package lock dependencies must be a non-empty object")

    checked: list[str] = []
    for package, expected_version in manifest_deps.items():
        if not isinstance(package, str) or not isinstance(expected_version, str):
            raise PackageLockError("manifest dependency names and versions must be strings")

        entry = lock_deps.get(package)
        if not isinstance(entry, dict):
            raise PackageLockError(f"direct dependency missing from lock: {package}")

        actual_version = entry.get("version")
        if actual_version != expected_version:
            raise PackageLockError(
                f"direct dependency version mismatch for {package}: "
                f"manifest={expected_version!r} lock={actual_version!r}"
            )

        depth = entry.get("depth")
        if depth != 0:
            raise PackageLockError(
                f"direct dependency must have depth 0 in lock: {package} depth={depth!r}"
            )

        expected_children = KNOWN_DIRECT_DEPENDENCIES.get(package)
        if expected_children is not None:
            actual_children = entry.get("dependencies")
            if actual_children != expected_children:
                raise PackageLockError(
                    f"known dependency contract mismatch for {package}: "
                    f"expected={expected_children!r} actual={actual_children!r}"
                )
            for child_package, child_version in expected_children.items():
                child_entry = lock_deps.get(child_package)
                if not isinstance(child_entry, dict):
                    raise PackageLockError(
                        f"resolved child dependency missing from lock: {package} -> {child_package}"
                    )
                if child_entry.get("version") != child_version:
                    raise PackageLockError(
                        f"resolved child dependency version mismatch for {package} -> {child_package}: "
                        f"expected={child_version!r} actual={child_entry.get('version')!r}"
                    )

        checked.append(package)

    return checked


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--manifest",
        type=Path,
        default=Path("unity_game/Packages/manifest.json"),
        help="Unity package manifest path",
    )
    parser.add_argument(
        "--lock",
        dest="lock_path",
        type=Path,
        default=Path("unity_game/Packages/packages-lock.json"),
        help="Unity package lock path",
    )
    return parser


def main() -> int:
    args = build_parser().parse_args()
    try:
        checked = verify(args.manifest, args.lock_path)
    except PackageLockError as exc:
        print(f"AFAREET_UNITY_PACKAGE_LOCK_ERROR: {exc}")
        return 2

    print(
        "AFAREET_UNITY_PACKAGE_LOCK_OK "
        f"directDependencies={len(checked)} packages={','.join(checked)}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
