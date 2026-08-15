#!/usr/bin/env python3
"""Verify Unity Test Framework NUnit XML contains real passing test evidence.

Designed for GameCI artifact folders where the exact XML filename may vary.
The verifier recursively finds XML files, accepts only NUnit <test-run> roots,
and fails closed unless at least one report exists and every discovered report
contains real passing evidence, zero failures/inconclusive tests, and fully
accounted result counters.
"""

from __future__ import annotations

import argparse
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

PASS_RESULTS = {"passed", "success"}


class TestEvidenceError(RuntimeError):
    pass


def _local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def _as_int(value: str | None, field: str, path: Path) -> int:
    try:
        return int(value or "0")
    except ValueError as exc:
        raise TestEvidenceError(f"{path}: {field} is not an integer: {value!r}") from exc


def verify_report(path: Path) -> dict[str, int | str]:
    try:
        root = ET.parse(path).getroot()
    except (OSError, ET.ParseError) as exc:
        raise TestEvidenceError(f"{path}: invalid XML: {exc}") from exc

    if _local_name(root.tag) != "test-run":
        raise TestEvidenceError(f"{path}: root is not NUnit test-run: {root.tag!r}")

    total = _as_int(root.attrib.get("total"), "total", path)
    passed = _as_int(root.attrib.get("passed"), "passed", path)
    failed = _as_int(root.attrib.get("failed"), "failed", path)
    skipped = _as_int(root.attrib.get("skipped"), "skipped", path)
    inconclusive = _as_int(root.attrib.get("inconclusive"), "inconclusive", path)
    result = (root.attrib.get("result") or "").strip()

    if total <= 0:
        raise TestEvidenceError(f"{path}: executed zero tests")
    if passed <= 0:
        raise TestEvidenceError(f"{path}: contains no passing tests; all-skipped evidence is not release eligible")
    if failed != 0:
        raise TestEvidenceError(f"{path}: contains failed tests: {failed}")
    if inconclusive != 0:
        raise TestEvidenceError(f"{path}: contains inconclusive tests: {inconclusive}")
    if result.lower() not in PASS_RESULTS:
        raise TestEvidenceError(f"{path}: result is not passing: {result!r}")
    if min(passed, failed, skipped, inconclusive) < 0:
        raise TestEvidenceError(
            f"{path}: counters cannot be negative: total={total} passed={passed} failed={failed} skipped={skipped} inconclusive={inconclusive}"
        )
    accounted = passed + failed + skipped + inconclusive
    if accounted != total:
        raise TestEvidenceError(
            f"{path}: counters do not account for every test: total={total} accounted={accounted} passed={passed} failed={failed} skipped={skipped} inconclusive={inconclusive}"
        )

    return {
        "path": str(path),
        "result": result,
        "total": total,
        "passed": passed,
        "failed": failed,
        "skipped": skipped,
        "inconclusive": inconclusive,
    }


def verify_artifact_tree(root: Path) -> list[dict[str, int | str]]:
    if not root.is_dir():
        raise TestEvidenceError(f"Unity test artifact directory is missing: {root}")

    xml_files = sorted(path for path in root.rglob("*.xml") if path.is_file())
    if not xml_files:
        raise TestEvidenceError(f"No XML test results found under: {root}")

    reports: list[dict[str, int | str]] = []
    non_test_xml: list[Path] = []
    for path in xml_files:
        try:
            parsed_root = ET.parse(path).getroot()
        except (OSError, ET.ParseError) as exc:
            raise TestEvidenceError(f"{path}: invalid XML: {exc}") from exc
        if _local_name(parsed_root.tag) != "test-run":
            non_test_xml.append(path)
            continue
        reports.append(verify_report(path))

    if not reports:
        extras = ", ".join(str(path) for path in non_test_xml[:5]) or "none"
        raise TestEvidenceError(
            f"No NUnit test-run XML found under {root}; non-test XML candidates: {extras}"
        )

    return reports


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Verify GameCI/Unity NUnit XML has non-empty passing test evidence.")
    parser.add_argument("--root", required=True, help="Artifact directory to scan recursively for NUnit XML")
    parser.add_argument("--label", default="Unity", help="Human-readable test mode label")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    root = Path(args.root).expanduser().resolve()
    try:
        reports = verify_artifact_tree(root)
        total = sum(int(report["total"]) for report in reports)
        passed = sum(int(report["passed"]) for report in reports)
        skipped = sum(int(report["skipped"]) for report in reports)
        print(
            "AFAREET_UNITY_TEST_EVIDENCE_OK "
            f"label={args.label} reports={len(reports)} total={total} passed={passed} skipped={skipped} root={root}"
        )
        return 0
    except TestEvidenceError as exc:
        print(f"AFAREET_UNITY_TEST_EVIDENCE_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
