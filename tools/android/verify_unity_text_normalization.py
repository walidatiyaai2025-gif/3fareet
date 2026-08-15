#!/usr/bin/env python3
"""Fail closed unless tracked Unity metadata uses the repository LF contract."""

from __future__ import annotations

import argparse
import subprocess
from pathlib import Path


PATTERNS = (
    "unity_game/ProjectSettings/*.asset",
    "unity_game/ProjectSettings/*.txt",
    "unity_game/Packages/*.json",
)


class TextNormalizationError(ValueError):
    pass


def _run_git(repo_root: Path, *args: str) -> bytes:
    try:
        completed = subprocess.run(
            ["git", "-C", str(repo_root), *args],
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
    except FileNotFoundError as exc:
        raise TextNormalizationError("git is required") from exc
    if completed.returncode != 0:
        stderr = completed.stderr.decode("utf-8", errors="replace").strip()
        raise TextNormalizationError(
            f"git {' '.join(args)} failed with exit code {completed.returncode}: {stderr}"
        )
    return completed.stdout


def _tracked_paths(repo_root: Path) -> list[str]:
    raw = _run_git(repo_root, "ls-files", "-z", "--", *PATTERNS)
    paths = [part.decode("utf-8") for part in raw.split(b"\0") if part]
    if not paths:
        raise TextNormalizationError(
            "no tracked Unity metadata files matched the LF normalization contract"
        )
    return paths


def _attributes(repo_root: Path, path: str) -> dict[str, str]:
    raw = _run_git(repo_root, "check-attr", "-z", "text", "eol", "--", path)
    parts = [part.decode("utf-8") for part in raw.split(b"\0") if part]
    if len(parts) % 3 != 0:
        raise TextNormalizationError(f"unexpected git check-attr output for {path!r}")
    attrs: dict[str, str] = {}
    for index in range(0, len(parts), 3):
        reported_path, key, value = parts[index : index + 3]
        if reported_path != path:
            raise TextNormalizationError(
                f"git check-attr returned unexpected path {reported_path!r} for {path!r}"
            )
        attrs[key] = value
    return attrs


def verify(repo_root: Path) -> list[str]:
    repo_root = repo_root.resolve()
    paths = _tracked_paths(repo_root)
    failures: list[str] = []

    for path in paths:
        attrs = _attributes(repo_root, path)
        if attrs.get("text") != "set":
            failures.append(f"{path}: text={attrs.get('text')!r}, expected 'set'")
        if attrs.get("eol") != "lf":
            failures.append(f"{path}: eol={attrs.get('eol')!r}, expected 'lf'")

        file_path = repo_root / path
        if not file_path.is_file():
            failures.append(f"{path}: tracked working-tree file is missing")
            continue
        if b"\r\n" in file_path.read_bytes():
            failures.append(f"{path}: working-tree content contains CRLF bytes")

    if failures:
        raise TextNormalizationError("; ".join(failures))
    return paths


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--repo-root",
        type=Path,
        default=Path("."),
        help="Repository root containing .git and unity_game/",
    )
    return parser


def main() -> int:
    args = build_parser().parse_args()
    try:
        paths = verify(args.repo_root)
    except TextNormalizationError as exc:
        print(f"AFAREET_UNITY_TEXT_NORMALIZATION_ERROR: {exc}")
        return 2

    print(f"AFAREET_UNITY_TEXT_NORMALIZATION_OK files={len(paths)} eol=lf")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
