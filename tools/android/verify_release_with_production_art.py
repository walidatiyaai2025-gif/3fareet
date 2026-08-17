#!/usr/bin/env python3
"""Authoritative P1 manual-publication preflight with production-art and smoke-metrics enforcement.

This wrapper binds the existing exact-candidate/device/review/approval publication
preflight to the fail-closed production-art gate and UPER-006 Android-observable
smoke metrics. It never publishes, tags, uploads, renames, or marks an APK VERIFIED.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

SCRIPT_DIR = Path(__file__).resolve().parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

import analyze_device_smoke
import prepare_candidate_device
import verify_p1_production_art
import verify_release_publication


class ReleaseWithProductionArtError(RuntimeError):
    pass


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise ReleaseWithProductionArtError(message)


def verify_release_with_art(
    *,
    candidate_manifest_path: Path,
    apk_path: Path | None,
    session_dir: Path,
    review_bundle_dir: Path,
    approvals_path: Path,
    gate_spec_path: Path,
    production_art_manifest_path: Path,
    production_art_spec_path: Path,
    repo_root: Path,
    performance_tier: str,
) -> dict[str, Any]:
    candidate_manifest_path = candidate_manifest_path.expanduser().resolve()
    apk_override = apk_path.expanduser().resolve() if apk_path is not None else None
    session_dir = session_dir.expanduser().resolve()
    requested_tier = performance_tier.lower()

    candidate_manifest = prepare_candidate_device.read_json(candidate_manifest_path)
    candidate = prepare_candidate_device.resolve_candidate(
        candidate_manifest,
        candidate_manifest_path,
        apk_override,
    )
    git_sha = str(candidate["gitSha"]).lower()
    apk_sha = str(candidate["apkSha256"]).lower()

    art = verify_p1_production_art.verify_art_manifest(
        manifest_path=production_art_manifest_path,
        repo_root=repo_root,
        spec_path=production_art_spec_path,
        expected_git_sha=git_sha,
        expected_apk_sha=apk_sha,
    )
    _require(art.get("verdict") == verify_p1_production_art.PASS_VERDICT, "production-art gate did not pass")
    _require(art.get("verified") is False, "production-art gate must not self-assert VERIFIED")

    smoke = analyze_device_smoke.analyze(session_dir, requested_tier)
    _require(smoke.get("verified") is False, "UPER-006 smoke analyzer must not self-assert VERIFIED")
    _require(
        smoke.get("verdict") == "PASSABLE_FOR_MANUAL_REVIEW",
        "UPER-006 Android-observable smoke metrics are blocked: " + "; ".join(smoke.get("blockers", [])),
    )
    _require(str(smoke.get("apkSha256") or "").lower() == apk_sha, "UPER-006 smoke APK SHA does not match candidate")
    _require(
        str(smoke.get("sessionPerformanceTier") or "").upper() == requested_tier.upper(),
        "UPER-006 smoke session performance tier does not match requested release tier",
    )

    release = verify_release_publication.verify_publication(
        candidate_manifest_path=candidate_manifest_path,
        apk_path=apk_override,
        session_dir=session_dir,
        review_bundle_dir=review_bundle_dir,
        approvals_path=approvals_path,
        spec_path=gate_spec_path,
    )
    _require(release.get("eligibleForManualPublication") is True, "release publication preflight is not eligible")
    _require(release.get("verified") is False, "release publication preflight must not self-assert VERIFIED")
    release_candidate = release.get("candidate")
    _require(isinstance(release_candidate, dict), "release publication result is missing candidate fingerprint")
    _require(str(release_candidate.get("gitSha") or "").lower() == git_sha, "release result Git SHA does not match production-art candidate")
    _require(str(release_candidate.get("apkSha256") or "").lower() == apk_sha, "release result APK SHA does not match production-art candidate")

    return {
        "schemaVersion": 2,
        "verdict": "ELIGIBLE_FOR_MANUAL_PUBLICATION_WITH_PRODUCTION_ART_AND_SMOKE_METRICS",
        "eligibleForManualPublication": True,
        "verified": False,
        "candidate": {"gitSha": git_sha, "apkSha256": apk_sha},
        "performanceTier": requested_tier.upper(),
        "productionArt": art,
        "uper006SmokeMetrics": smoke,
        "releasePreflight": release,
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Require exact-candidate production-art acceptance, UPER-006 smoke metrics and P1 release publication preflight."
    )
    parser.add_argument("--candidate-manifest", required=True)
    parser.add_argument("--apk", help="Optional exact APK override if the candidate bundle moved workstations.")
    parser.add_argument("--session", required=True)
    parser.add_argument("--review-bundle", required=True)
    parser.add_argument("--approvals", required=True)
    parser.add_argument("--spec", default=str(verify_release_publication.p1_gate_readiness.DEFAULT_SPEC))
    parser.add_argument("--production-art-manifest", required=True)
    parser.add_argument("--production-art-spec", default=str(verify_p1_production_art.DEFAULT_SPEC))
    parser.add_argument("--repo-root", default=".")
    parser.add_argument(
        "--performance-tier",
        required=True,
        choices=("low", "mid", "high"),
        help="UPER-001 capability tier whose Android-observable smoke budgets must be enforced.",
    )
    parser.add_argument("--output", help="Optional combined preflight JSON; existing files are never overwritten.")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        result = verify_release_with_art(
            candidate_manifest_path=Path(args.candidate_manifest),
            apk_path=Path(args.apk) if args.apk else None,
            session_dir=Path(args.session),
            review_bundle_dir=Path(args.review_bundle),
            approvals_path=Path(args.approvals),
            gate_spec_path=Path(args.spec),
            production_art_manifest_path=Path(args.production_art_manifest),
            production_art_spec_path=Path(args.production_art_spec),
            repo_root=Path(args.repo_root),
            performance_tier=args.performance_tier,
        )
        output = None
        if args.output:
            output = Path(args.output).expanduser().resolve()
            output.parent.mkdir(parents=True, exist_ok=True)
            if output.exists():
                raise ReleaseWithProductionArtError(f"refusing to overwrite existing combined preflight: {output}")
            output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(
            "AFAREET_RELEASE_WITH_PRODUCTION_ART_OK "
            f"gitSha={result['candidate']['gitSha']} apkSha256={result['candidate']['apkSha256']} "
            f"performanceTier={result['performanceTier']} verdict={result['verdict']} verified=false"
            + (f" output={output}" if output else "")
        )
        return 0
    except (
        ReleaseWithProductionArtError,
        verify_p1_production_art.ProductionArtGateError,
        verify_release_publication.PublicationPreflightError,
        prepare_candidate_device.CandidatePrepareError,
        RuntimeError,
        OSError,
        ValueError,
        json.JSONDecodeError,
    ) as exc:
        print(f"AFAREET_RELEASE_WITH_PRODUCTION_ART_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
