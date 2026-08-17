import hashlib
import importlib.util
import json
import shutil
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "prepare_p1_candidate_device.py"
SPEC = importlib.util.spec_from_file_location("prepare_p1_candidate_device", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
P1 = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(P1)

STAGING_SHA = "a" * 40
CANDIDATE_SHA = "b" * 40
HANDOFF_PACKET_SHA = "c" * 64
NATIVE_HANDOFF_VERIFICATION_SHA = "d" * 64
OPERATOR_CHAIN_SHA = "e" * 64
TASKS = ["UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"]
AUTHORIZATION = {
    "authorizationSourceGitSha": STAGING_SHA,
    "handoffPacketSha256": HANDOFF_PACKET_SHA,
    "nativeHandoffVerificationSha256": NATIVE_HANDOFF_VERIFICATION_SHA,
    "operatorChainSha256": OPERATOR_CHAIN_SHA,
}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_json(path: Path, payload) -> None:
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def make_bundle(root: Path) -> dict[str, Path]:
    apk = root / "afareet-unity3d-debug.apk"
    apk.write_bytes(b"p1-device-apk")
    apk_hash = sha256(apk)

    candidate = root / "local-candidate-manifest.json"
    write_json(
        candidate,
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
                "path": str(apk),
                "fileName": "afareet-unity3d-debug.apk",
                "sizeBytes": apk.stat().st_size,
                "sha256": apk_hash,
            },
        },
    )

    evidence = []
    states = {
        "UART-003": "LICENSED_UNITY_STAGE_AND_BIND_OK",
        "UART-004": "LICENSED_UNITY_STAGE_AND_BIND_OK",
        "UART-005": "LICENSED_UNITY_IMPORT_STAGE_OK",
        "UART-006": "LICENSED_UNITY_IMPORT_STAGE_OK",
        "UART-007": "LICENSED_UNITY_IMPORT_STAGE_OK",
        "URAC-011": "LICENSED_UNITY_TRACKED_LAYOUT_IMPORT_OK",
    }
    for task in TASKS:
        evidence.append(
            {
                "taskId": task,
                "state": states[task],
                "sourceEvidence": f"source:{task}",
                "runtimeEvidence": f"runtime:{task}",
                "verified": False,
                "runtimeVerified": False,
                "ownerAccepted": False,
            }
        )

    staging = root / "p1-staging-handoff.json"
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
            "coveredTasks": TASKS,
            "taskEvidence": evidence,
            "verified": False,
            "runtimeVerified": False,
            "ownerAccepted": False,
            "publicationEligible": False,
            "candidateBuildStarted": False,
        },
    )

    lineage = root / "p1-staging-lineage.json"
    write_json(
        lineage,
        {
            "schemaVersion": 1,
            "state": "STAGING_PARENT_BOUND_TO_CANDIDATE",
            "stagingSourceGitSha": STAGING_SHA,
            "candidateGitSha": CANDIDATE_SHA,
            "directParentGitSha": STAGING_SHA,
            "stagingReportSha256": sha256(staging),
            "stagingAuthorization": dict(AUTHORIZATION),
            "coveredTasks": TASKS,
            "readyForLicensedCandidateTests": True,
            "verified": False,
            "runtimeVerified": False,
            "ownerAccepted": False,
            "publicationEligible": False,
        },
    )

    p1_manifest = root / "p1-staged-candidate-manifest.json"
    write_json(
        p1_manifest,
        {
            "schemaVersion": 1,
            "candidateType": "p1-staged-local-windows-licensed-unity",
            "stagingSourceGitSha": STAGING_SHA,
            "candidateGitSha": CANDIDATE_SHA,
            "directParentGitSha": STAGING_SHA,
            "stagingAuthorization": dict(AUTHORIZATION),
            "stagingReport": {
                "path": str(staging),
                "sha256": sha256(staging),
                "schemaVersion": 3,
            },
            "stagingLineage": {
                "path": str(lineage),
                "sha256": sha256(lineage),
                "state": "STAGING_PARENT_BOUND_TO_CANDIDATE",
            },
            "localCandidateManifest": {
                "path": str(candidate),
                "sha256": sha256(candidate),
            },
            "apkSha256": apk_hash,
            "coveredTasks": TASKS,
            "readyForDeviceEvidence": True,
            "verified": False,
            "runtimeVerified": False,
            "ownerAccepted": False,
            "publicationEligible": False,
            "verdict": "P1_STAGED_CANDIDATE_READY_FOR_PHYSICAL_DEVICE_EVIDENCE",
        },
    )
    return {
        "apk": apk,
        "candidate": candidate,
        "staging": staging,
        "lineage": lineage,
        "p1": p1_manifest,
    }


class FakeDeviceEvidence:
    @staticmethod
    def write_json(path: Path, payload) -> None:
        write_json(path, payload)


class PrepareP1CandidateDeviceTests(unittest.TestCase):
    def test_valid_chain_binds_exact_staging_candidate_and_apk(self):
        with tempfile.TemporaryDirectory() as tmp:
            bundle = make_bundle(Path(tmp))
            chain = P1.validate_p1_chain(bundle["p1"])
            self.assertEqual(STAGING_SHA, chain["stagingSourceGitSha"])
            self.assertEqual(CANDIDATE_SHA, chain["candidateGitSha"])
            self.assertEqual(STAGING_SHA, chain["directParentGitSha"])
            self.assertEqual(sha256(bundle["apk"]), chain["apkSha256"])
            self.assertEqual(TASKS, chain["coveredTasks"])
            self.assertEqual(AUTHORIZATION, chain["stagingAuthorization"])
            self.assertEqual(bundle["candidate"].resolve(), chain["candidateManifestPath"])

    def test_staging_report_tamper_is_rejected_before_device_prepare(self):
        with tempfile.TemporaryDirectory() as tmp:
            bundle = make_bundle(Path(tmp))
            payload = json.loads(bundle["staging"].read_text(encoding="utf-8"))
            payload["candidateBuildStarted"] = True
            write_json(bundle["staging"], payload)
            with self.assertRaisesRegex(P1.P1CandidatePrepareError, "stagingReport SHA-256 mismatch"):
                P1.validate_p1_chain(bundle["p1"])

    def test_authorization_mismatch_is_rejected_before_device_prepare(self):
        with tempfile.TemporaryDirectory() as tmp:
            bundle = make_bundle(Path(tmp))
            lineage = json.loads(bundle["lineage"].read_text(encoding="utf-8"))
            lineage["stagingAuthorization"]["operatorChainSha256"] = "f" * 64
            write_json(bundle["lineage"], lineage)
            envelope = json.loads(bundle["p1"].read_text(encoding="utf-8"))
            envelope["stagingLineage"]["sha256"] = sha256(bundle["lineage"])
            write_json(bundle["p1"], envelope)
            with self.assertRaisesRegex(P1.P1CandidatePrepareError, "authorization fingerprints"):
                P1.validate_p1_chain(bundle["p1"])

    def test_lineage_tamper_is_rejected_before_device_prepare(self):
        with tempfile.TemporaryDirectory() as tmp:
            bundle = make_bundle(Path(tmp))
            payload = json.loads(bundle["lineage"].read_text(encoding="utf-8"))
            payload["candidateGitSha"] = "c" * 40
            write_json(bundle["lineage"], payload)
            with self.assertRaisesRegex(P1.P1CandidatePrepareError, "stagingLineage SHA-256 mismatch"):
                P1.validate_p1_chain(bundle["p1"])

    def test_generic_candidate_sha_mismatch_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            bundle = make_bundle(root)
            candidate = json.loads(bundle["candidate"].read_text(encoding="utf-8"))
            candidate["gitSha"] = "c" * 40
            write_json(bundle["candidate"], candidate)
            envelope = json.loads(bundle["p1"].read_text(encoding="utf-8"))
            envelope["localCandidateManifest"]["sha256"] = sha256(bundle["candidate"])
            write_json(bundle["p1"], envelope)
            with self.assertRaisesRegex(P1.P1CandidatePrepareError, "Generic candidate SHA does not match P1 lineage"):
                P1.validate_p1_chain(bundle["p1"])

    def test_p1_manifest_cannot_self_assert_verification_or_expand_scope(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            bundle = make_bundle(root)
            envelope = json.loads(bundle["p1"].read_text(encoding="utf-8"))
            envelope["verified"] = True
            write_json(bundle["p1"], envelope)
            with self.assertRaisesRegex(P1.P1CandidatePrepareError, "p1Manifest.verified must remain JSON false"):
                P1.validate_p1_chain(bundle["p1"])

            bundle = make_bundle(root)
            envelope = json.loads(bundle["p1"].read_text(encoding="utf-8"))
            envelope["coveredTasks"] = TASKS + ["UPER-009"]
            write_json(bundle["p1"], envelope)
            with self.assertRaisesRegex(P1.P1CandidatePrepareError, "ordered six-task"):
                P1.validate_p1_chain(bundle["p1"])

    def test_moved_bundle_overrides_still_require_exact_bytes(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            bundle = make_bundle(root)
            moved = root / "moved"
            moved.mkdir()
            moved_candidate = moved / bundle["candidate"].name
            moved_staging = moved / bundle["staging"].name
            moved_lineage = moved / bundle["lineage"].name
            moved_apk = moved / bundle["apk"].name
            for source, target in (
                (bundle["candidate"], moved_candidate),
                (bundle["staging"], moved_staging),
                (bundle["lineage"], moved_lineage),
                (bundle["apk"], moved_apk),
            ):
                shutil.copy2(source, target)
                source.unlink()
            chain = P1.validate_p1_chain(
                bundle["p1"],
                candidate_manifest_override=moved_candidate,
                staging_report_override=moved_staging,
                staging_lineage_override=moved_lineage,
                apk_override=moved_apk,
            )
            self.assertEqual(moved_apk.resolve(), chain["candidate"]["apkPath"])
            moved_staging.write_bytes(moved_staging.read_bytes() + b"tamper")
            with self.assertRaises(P1.P1CandidatePrepareError):
                P1.validate_p1_chain(
                    bundle["p1"],
                    candidate_manifest_override=moved_candidate,
                    staging_report_override=moved_staging,
                    staging_lineage_override=moved_lineage,
                    apk_override=moved_apk,
                )

    def test_session_binding_copies_p1_provenance_and_keeps_all_approval_flags_false(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            bundle = make_bundle(root)
            chain = P1.validate_p1_chain(bundle["p1"])
            session_dir = root / "session"
            session_dir.mkdir()
            write_json(
                session_dir / P1.prepare_candidate_device.SESSION_FILE,
                {
                    "packageId": P1.prepare_candidate_device.EXPECTED_PACKAGE_ID,
                    "candidate": {
                        "gitSha": CANDIDATE_SHA,
                        "apkSha256": chain["apkSha256"],
                        "verified": False,
                    },
                    "performanceTier": "mid",
                },
            )
            (session_dir / P1.prepare_candidate_device.BOUND_MANIFEST_FILE).write_bytes(bundle["candidate"].read_bytes())

            context = P1.bind_p1_chain_to_session(chain, session_dir, FakeDeviceEvidence)
            saved = json.loads((session_dir / "session.json").read_text(encoding="utf-8"))
            self.assertEqual("P1_LINEAGE_BOUND_FOR_PHYSICAL_DEVICE_EVIDENCE", saved["p1Lineage"]["state"])
            self.assertEqual(CANDIDATE_SHA, saved["p1Lineage"]["candidateGitSha"])
            self.assertEqual(TASKS, saved["p1Lineage"]["coveredTasks"])
            self.assertEqual(AUTHORIZATION, saved["p1Lineage"]["stagingAuthorization"])
            self.assertEqual(AUTHORIZATION, context["stagingAuthorization"])
            self.assertTrue(saved["p1Lineage"]["readyForCheckpointCapture"])
            for key in ("verified", "runtimeVerified", "ownerAccepted", "publicationEligible"):
                self.assertFalse(saved["p1Lineage"][key])
                self.assertFalse(context[key])
            for file_name in (
                P1.BOUND_P1_MANIFEST_FILE,
                P1.BOUND_STAGING_REPORT_FILE,
                P1.BOUND_LINEAGE_REPORT_FILE,
                P1.prepare_candidate_device.BOUND_MANIFEST_FILE,
            ):
                self.assertTrue((session_dir / file_name).is_file())
            self.assertEqual(bundle["p1"].read_bytes(), (session_dir / P1.BOUND_P1_MANIFEST_FILE).read_bytes())
            self.assertEqual(bundle["staging"].read_bytes(), (session_dir / P1.BOUND_STAGING_REPORT_FILE).read_bytes())
            self.assertEqual(bundle["lineage"].read_bytes(), (session_dir / P1.BOUND_LINEAGE_REPORT_FILE).read_bytes())

    def test_parser_requires_p1_manifest_and_performance_tier(self):
        parser = P1.build_parser()
        with self.assertRaises(SystemExit):
            parser.parse_args(["--p1-candidate-manifest", "p1.json", "--output", "evidence"])
        args = parser.parse_args(
            [
                "--p1-candidate-manifest",
                "p1.json",
                "--output",
                "evidence",
                "--performance-tier",
                "high",
            ]
        )
        self.assertEqual("high", args.performance_tier)


if __name__ == "__main__":
    unittest.main()
