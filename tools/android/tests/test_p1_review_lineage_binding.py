import hashlib
import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


TOOLS = Path(__file__).resolve().parents[1]


def load(name: str):
    path = TOOLS / f"{name}.py"
    spec = importlib.util.spec_from_file_location(name, path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


GENERIC_VERIFY = load("verify_device_review_bundle")
EXPORT = load("export_p1_device_evidence")
VERIFY = load("verify_p1_device_review_bundle")

STAGING_SHA = "a" * 40
CANDIDATE_SHA = "b" * 40
APK_SHA = "c" * 64
HANDOFF_PACKET_SHA = "d" * 64
NATIVE_HANDOFF_VERIFICATION_SHA = "e" * 64
OPERATOR_CHAIN_SHA = "f" * 64
TASKS = ["UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"]
AUTHORIZATION = {
    "authorizationSourceGitSha": STAGING_SHA,
    "handoffPacketSha256": HANDOFF_PACKET_SHA,
    "nativeHandoffVerificationSha256": NATIVE_HANDOFF_VERIFICATION_SHA,
    "operatorChainSha256": OPERATOR_CHAIN_SHA,
}
SERIAL = "ADB-P1-SECRET-SERIAL-98765"
SERIAL_SHA = hashlib.sha256(SERIAL.encode("utf-8")).hexdigest()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_json(path: Path, payload) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def build_session(root: Path, *, red_flags: int = 0) -> Path:
    session = root / "raw-p1-session"
    checkpoint = session / "checkpoints" / "results"
    checkpoint.mkdir(parents=True)

    candidate_manifest = session / "candidate-manifest.json"
    write_json(
        candidate_manifest,
        {
            "schemaVersion": 1,
            "candidateType": "local-windows-licensed-unity",
            "gitSha": CANDIDATE_SHA,
            "packageId": "com.fiftysolutions.afareetunity3d",
            "releaseEvidenceEligible": True,
            "readyForDeviceEvidence": True,
            "verified": False,
            "verdict": "READY_FOR_PHYSICAL_DEVICE_EVIDENCE",
            "apk": {
                "path": f"C:/private/{SERIAL}/afareet-unity3d-debug.apk",
                "fileName": "afareet-unity3d-debug.apk",
                "sizeBytes": 123,
                "sha256": APK_SHA,
            },
        },
    )
    candidate_manifest_sha = sha256(candidate_manifest)

    evidence_states = {
        "UART-003": "LICENSED_UNITY_STAGE_AND_BIND_OK",
        "UART-004": "LICENSED_UNITY_STAGE_AND_BIND_OK",
        "UART-005": "LICENSED_UNITY_IMPORT_STAGE_OK",
        "UART-006": "LICENSED_UNITY_IMPORT_STAGE_OK",
        "UART-007": "LICENSED_UNITY_IMPORT_STAGE_OK",
        "URAC-011": "LICENSED_UNITY_TRACKED_LAYOUT_IMPORT_OK",
    }
    task_evidence = [
        {
            "taskId": task,
            "state": evidence_states[task],
            "sourceEvidence": f"Assets/private/{task}",
            "runtimeEvidence": f"runtime:{task}",
            "verified": False,
            "runtimeVerified": False,
            "ownerAccepted": False,
        }
        for task in TASKS
    ]

    staging = session / "p1-staging-handoff.json"
    write_json(
        staging,
        {
            "schemaVersion": 3,
            "state": "STAGED_FOR_COMMIT_NOT_CANDIDATE",
            "gitSha": STAGING_SHA,
            "authorizationSourceGitSha": STAGING_SHA,
            "handoffPacketSha256": HANDOFF_PACKET_SHA,
            "nativeHandoffVerificationSha256": NATIVE_HANDOFF_VERIFICATION_SHA,
            "operatorChainSha256": OPERATOR_CHAIN_SHA,
            "heroSource": "Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing.fbx",
            "coveredTasks": TASKS,
            "taskEvidence": task_evidence,
            "verified": False,
            "runtimeVerified": False,
            "ownerAccepted": False,
            "publicationEligible": False,
            "candidateBuildStarted": False,
        },
    )

    lineage = session / "p1-staging-lineage.json"
    write_json(
        lineage,
        {
            "schemaVersion": 1,
            "state": "STAGING_PARENT_BOUND_TO_CANDIDATE",
            "stagingReportSchemaVersion": 3,
            "stagingSourceGitSha": STAGING_SHA,
            "candidateGitSha": CANDIDATE_SHA,
            "directParentGitSha": STAGING_SHA,
            "stagingReportSha256": sha256(staging),
            "stagingAuthorization": dict(AUTHORIZATION),
            "coveredTasks": TASKS,
            "candidateCommitChangedPaths": [
                "unity_game/Assets/Afareet/Resources/Art/Vehicles/HeroCar/Production/PF_AfareetKing.prefab"
            ],
            "readyForLicensedCandidateTests": True,
            "verified": False,
            "runtimeVerified": False,
            "ownerAccepted": False,
            "publicationEligible": False,
        },
    )

    p1_manifest = session / "p1-staged-candidate-manifest.json"
    write_json(
        p1_manifest,
        {
            "schemaVersion": 1,
            "candidateType": "p1-staged-local-windows-licensed-unity",
            "generatedAtUtc": "2026-08-17T00:00:00Z",
            "stagingSourceGitSha": STAGING_SHA,
            "candidateGitSha": CANDIDATE_SHA,
            "directParentGitSha": STAGING_SHA,
            "stagingAuthorization": dict(AUTHORIZATION),
            "stagingReport": {
                "path": f"C:/private/{SERIAL}/p1-staging-handoff.json",
                "sha256": sha256(staging),
                "schemaVersion": 3,
            },
            "stagingLineage": {
                "path": f"C:/private/{SERIAL}/p1-staging-lineage.json",
                "sha256": sha256(lineage),
                "state": "STAGING_PARENT_BOUND_TO_CANDIDATE",
            },
            "localCandidateManifest": {
                "path": f"C:/private/{SERIAL}/local-candidate-manifest.json",
                "sha256": candidate_manifest_sha,
            },
            "apkSha256": APK_SHA,
            "coveredTasks": TASKS,
            "readyForDeviceEvidence": True,
            "verified": False,
            "runtimeVerified": False,
            "ownerAccepted": False,
            "publicationEligible": False,
            "verdict": "P1_STAGED_CANDIDATE_READY_FOR_PHYSICAL_DEVICE_EVIDENCE",
        },
    )

    file_hashes = {
        "p1Manifest": sha256(p1_manifest),
        "stagingReport": sha256(staging),
        "stagingLineage": sha256(lineage),
        "candidateManifest": candidate_manifest_sha,
    }
    session_payload = {
        "packageId": "com.fiftysolutions.afareetunity3d",
        "apk": {"sha256": APK_SHA},
        "device": {"serial": SERIAL, "serialSha256": SERIAL_SHA},
        "performanceTier": "mid",
        "candidate": {
            "schemaVersion": 1,
            "candidateType": "local-windows-licensed-unity",
            "gitSha": CANDIDATE_SHA,
            "apkSha256": APK_SHA,
            "releaseEvidenceEligible": True,
            "readyForDeviceEvidence": True,
            "verified": False,
            "verdict": "READY_FOR_PHYSICAL_DEVICE_EVIDENCE",
            "manifest": {
                "fileName": "candidate-manifest.json",
                "sourceFileName": "local-candidate-manifest.json",
                "sha256": candidate_manifest_sha,
            },
        },
        "p1Lineage": {
            "schemaVersion": 1,
            "state": "P1_LINEAGE_BOUND_FOR_PHYSICAL_DEVICE_EVIDENCE",
            "stagingSourceGitSha": STAGING_SHA,
            "candidateGitSha": CANDIDATE_SHA,
            "directParentGitSha": STAGING_SHA,
            "apkSha256": APK_SHA,
            "coveredTasks": TASKS,
            "stagingAuthorization": dict(AUTHORIZATION),
            "files": {
                "p1Manifest": {"fileName": "p1-staged-candidate-manifest.json", "sha256": file_hashes["p1Manifest"]},
                "stagingReport": {"fileName": "p1-staging-handoff.json", "sha256": file_hashes["stagingReport"]},
                "stagingLineage": {"fileName": "p1-staging-lineage.json", "sha256": file_hashes["stagingLineage"]},
                "candidateManifest": {"fileName": "candidate-manifest.json", "sha256": file_hashes["candidateManifest"]},
            },
            "readyForCheckpointCapture": True,
            "verified": False,
            "runtimeVerified": False,
            "ownerAccepted": False,
            "publicationEligible": False,
        },
    }
    write_json(session / "session.json", session_payload)
    (session / "package-dump.txt").write_text(f"secret={SERIAL}\n", encoding="utf-8")

    index = {
        "schemaVersion": 1,
        "state": "EVIDENCE_COLLECTED",
        "verdict": "MANUAL_REVIEW_REQUIRED",
        "packageId": "com.fiftysolutions.afareetunity3d",
        "apkSha256": APK_SHA,
        "deviceSerialSha256": SERIAL_SHA,
        "device": {
            "manufacturer": "Acme",
            "model": "Physical P1 Phone",
            "androidRelease": "16",
            "apiLevel": "36",
            "primaryAbi": "arm64-v8a",
            "isEmulator": False,
        },
        "checkpointCount": 1,
        "checkpoints": ["results"],
        "automatedRedFlagCount": red_flags,
        "automatedRedFlags": ([{"checkpoint": "results", "finding": "fixture red flag"}] if red_flags else []),
        "manualReviewChecklist": ["P1 manual review remains required"],
    }
    write_json(session / "evidence-index.json", index)

    checkpoint_json = {
        "schemaVersion": 1,
        "label": "results",
        "apkSha256": APK_SHA,
        "deviceSerialSha256": SERIAL_SHA,
        "automatedRedFlags": [],
        "automatedRedFlagCount": 0,
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
    write_json(checkpoint / "checkpoint.json", checkpoint_json)
    (checkpoint / "screen.png").write_bytes(b"\x89PNG\r\n\x1a\nP1")
    for name in ("meminfo.txt", "gfxinfo.txt", "thermalservice.txt", "battery.txt"):
        (checkpoint / name).write_text(f"safe {name}\n", encoding="utf-8")
    (checkpoint / "logcat.txt").write_text(f"private {SERIAL}\n", encoding="utf-8")
    (checkpoint / "activity.txt").write_text(f"private {SERIAL}\n", encoding="utf-8")
    return session


def rewrite_manifest_for_summary(bundle: Path, summary_path: Path) -> dict:
    manifest_path = bundle / "review-manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    record = EXPORT.export_device_evidence._content_record(summary_path)
    manifest["contentFiles"]["p1-review-lineage.json"] = record
    manifest["p1Lineage"]["sha256"] = record["sha256"]
    manifest["contentSetSha256"] = EXPORT.export_device_evidence._content_set_sha256(manifest["contentFiles"])
    write_json(manifest_path, manifest)
    return manifest


class P1ReviewLineageBindingTests(unittest.TestCase):
    def test_valid_p1_export_remains_generic_verifiable_and_is_p1_verifiable(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = build_session(root)
            bundle = root / "review"
            manifest = EXPORT.export_p1_bundle(session, bundle)

            self.assertEqual("p1-final-gate-lineage-v2", manifest["reviewProfile"])
            self.assertEqual(2, manifest["p1Lineage"]["schemaVersion"])
            self.assertEqual("P1_REVIEW_LINEAGE_ATTACHED", manifest["p1Lineage"]["state"])
            self.assertEqual(AUTHORIZATION, manifest["p1Lineage"]["stagingAuthorization"])
            generic = GENERIC_VERIFY.verify_bundle(bundle, expected_git_sha=CANDIDATE_SHA, expected_apk_sha=APK_SHA)
            p1 = VERIFY.verify_p1_bundle(
                bundle,
                expected_git_sha=CANDIDATE_SHA,
                expected_apk_sha=APK_SHA,
                expected_staging_source_sha=STAGING_SHA,
            )
            self.assertEqual(generic["contentSetSha256"], p1["contentSetSha256"])
            self.assertEqual(STAGING_SHA, p1["stagingSourceGitSha"])
            self.assertEqual(CANDIDATE_SHA, p1["candidateGitSha"])
            self.assertEqual(TASKS, p1["coveredTasks"])
            self.assertEqual(AUTHORIZATION, p1["stagingAuthorization"])
            self.assertEqual("mid", p1["performanceTier"])
            self.assertFalse(p1["verified"])
            self.assertFalse(p1["runtimeVerified"])
            self.assertFalse(p1["ownerAccepted"])
            self.assertFalse(p1["publicationEligible"])

    def test_public_bundle_contains_only_sanitized_p1_summary_not_raw_provenance(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = build_session(root)
            bundle = root / "review"
            EXPORT.export_p1_bundle(session, bundle)

            summary_path = bundle / "p1-review-lineage.json"
            self.assertTrue(summary_path.is_file())
            summary = json.loads(summary_path.read_text(encoding="utf-8"))
            self.assertEqual(AUTHORIZATION, summary["stagingAuthorization"])
            self.assertTrue(summary["privacy"]["authorizationContainsOnlyDigests"])
            for forbidden in (
                "p1-staged-candidate-manifest.json",
                "p1-staging-handoff.json",
                "p1-staging-lineage.json",
                "candidate-manifest.json",
                "session.json",
            ):
                self.assertFalse((bundle / forbidden).exists(), forbidden)
            for path in bundle.rglob("*"):
                if path.is_file() and path.suffix.lower() in {".json", ".txt", ".md", ".csv", ".xml"}:
                    text = path.read_text(encoding="utf-8", errors="replace")
                    self.assertNotIn(SERIAL, text)
                    self.assertNotIn("C:/private/", text)
                    self.assertNotIn("Assets/private/", text)

    def test_bound_raw_p1_file_tamper_is_rejected_before_public_export(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = build_session(root)
            staging = session / "p1-staging-handoff.json"
            staging.write_bytes(staging.read_bytes() + b"tamper")
            with self.assertRaisesRegex(EXPORT.P1EvidenceExportError, "provenance SHA mismatch"):
                EXPORT.export_p1_bundle(session, root / "review")
            self.assertFalse((root / "review").exists())

    def test_raw_authorization_mismatch_is_rejected_before_public_export(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = build_session(root)
            session_path = session / "session.json"
            payload = json.loads(session_path.read_text(encoding="utf-8"))
            payload["p1Lineage"]["stagingAuthorization"]["operatorChainSha256"] = "1" * 64
            write_json(session_path, payload)
            with self.assertRaisesRegex(EXPORT.P1EvidenceExportError, "authorization differs"):
                EXPORT.export_p1_bundle(session, root / "review")
            self.assertFalse((root / "review").exists())

    def test_joint_session_hash_tamper_cannot_self_assert_staging_verification(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = build_session(root)
            staging_path = session / "p1-staging-handoff.json"
            staging = json.loads(staging_path.read_text(encoding="utf-8"))
            staging["taskEvidence"][0]["verified"] = True
            write_json(staging_path, staging)

            lineage_path = session / "p1-staging-lineage.json"
            lineage = json.loads(lineage_path.read_text(encoding="utf-8"))
            lineage["stagingReportSha256"] = sha256(staging_path)
            write_json(lineage_path, lineage)

            p1_path = session / "p1-staged-candidate-manifest.json"
            envelope = json.loads(p1_path.read_text(encoding="utf-8"))
            envelope["stagingReport"]["sha256"] = sha256(staging_path)
            envelope["stagingLineage"]["sha256"] = sha256(lineage_path)
            write_json(p1_path, envelope)

            session_path = session / "session.json"
            payload = json.loads(session_path.read_text(encoding="utf-8"))
            payload["p1Lineage"]["files"]["stagingReport"]["sha256"] = sha256(staging_path)
            payload["p1Lineage"]["files"]["stagingLineage"]["sha256"] = sha256(lineage_path)
            payload["p1Lineage"]["files"]["p1Manifest"]["sha256"] = sha256(p1_path)
            write_json(session_path, payload)

            with self.assertRaises(EXPORT.P1EvidenceExportError):
                EXPORT.export_p1_bundle(session, root / "review")

    def test_sanitized_p1_lineage_tamper_is_caught_by_generic_content_set(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = build_session(root)
            bundle = root / "review"
            EXPORT.export_p1_bundle(session, bundle)
            summary = bundle / "p1-review-lineage.json"
            summary.write_bytes(summary.read_bytes() + b"tamper")
            with self.assertRaises(GENERIC_VERIFY.ReviewBundleVerificationError):
                GENERIC_VERIFY.verify_bundle(bundle)
            with self.assertRaises(GENERIC_VERIFY.ReviewBundleVerificationError):
                VERIFY.verify_p1_bundle(bundle)

    def test_sanitized_authorization_tamper_is_rejected_even_after_content_hash_rewrite(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = build_session(root)
            bundle = root / "review"
            EXPORT.export_p1_bundle(session, bundle)

            summary_path = bundle / "p1-review-lineage.json"
            summary = json.loads(summary_path.read_text(encoding="utf-8"))
            summary["stagingAuthorization"]["operatorChainSha256"] = "1" * 64
            write_json(summary_path, summary)
            rewrite_manifest_for_summary(bundle, summary_path)

            GENERIC_VERIFY.verify_bundle(bundle)
            with self.assertRaisesRegex(VERIFY.P1ReviewBundleVerificationError, "authorization differs"):
                VERIFY.verify_p1_bundle(bundle)

    def test_legacy_review_profile_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = build_session(root)
            bundle = root / "review"
            EXPORT.export_p1_bundle(session, bundle)
            manifest_path = bundle / "review-manifest.json"
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
            manifest["reviewProfile"] = "p1-final-gate-lineage-v1"
            write_json(manifest_path, manifest)
            with self.assertRaisesRegex(VERIFY.P1ReviewBundleVerificationError, "reviewProfile"):
                VERIFY.verify_p1_bundle(bundle)

    def test_generic_review_bundle_without_p1_binding_is_rejected_by_p1_verifier(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = build_session(root)
            bundle = root / "generic-review"
            EXPORT.export_device_evidence.export_bundle(session, bundle)
            with self.assertRaisesRegex(VERIFY.P1ReviewBundleVerificationError, "reviewProfile"):
                VERIFY.verify_p1_bundle(bundle)

    def test_p1_review_scope_or_approval_self_assertion_is_rejected_even_if_content_hashes_are_rewritten(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = build_session(root)
            bundle = root / "review"
            EXPORT.export_p1_bundle(session, bundle)

            summary_path = bundle / "p1-review-lineage.json"
            summary = json.loads(summary_path.read_text(encoding="utf-8"))
            summary["coveredTasks"] = TASKS + ["UPER-009"]
            summary["verified"] = True
            write_json(summary_path, summary)
            rewrite_manifest_for_summary(bundle, summary_path)

            GENERIC_VERIFY.verify_bundle(bundle)
            with self.assertRaises(VERIFY.P1ReviewBundleVerificationError):
                VERIFY.verify_p1_bundle(bundle)

    def test_export_cli_preserves_nonzero_red_flag_semantics_after_p1_augmentation(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = build_session(root, red_flags=1)
            bundle = root / "review"
            code = EXPORT.main(["--session", str(session), "--output", str(bundle)])
            self.assertEqual(2, code)
            self.assertTrue((bundle / "p1-review-lineage.json").is_file())
            manifest = json.loads((bundle / "review-manifest.json").read_text(encoding="utf-8"))
            self.assertEqual(1, manifest["automatedRedFlagCount"])
            self.assertEqual("MANUAL_REVIEW_REQUIRED", manifest["verdict"])


if __name__ == "__main__":
    unittest.main()
