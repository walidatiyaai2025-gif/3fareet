import json
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
CHAIN_PATH = ROOT / "tools" / "android" / "p1_operator_release_chain.json"
POLICY_PATH = ROOT / "docs" / "RELEASE_POLICY.md"

EXPECTED_STAGE_IDS = [
    "UART003_PORTABLE_HERO_INTAKE",
    "P1_VISUAL_SOURCE_READINESS",
    "P1_LICENSED_STAGING_READINESS",
    "UART003_NATIVE_WINDOWS_INTAKE",
    "P1_LICENSED_UNITY_STAGING",
    "P1_REVIEW_AND_COMMIT_STAGING_DELTA",
    "P1_STAGED_CANDIDATE",
    "P1_DEVICE_SESSION_BINDING",
    "P1_PHYSICAL_DEVICE_EVIDENCE",
    "P1_SANITIZED_REVIEW_EXPORT",
    "P1_REVIEW_LINEAGE_VERIFICATION",
    "P1_LINEAGE_GATE_READINESS",
    "P1_PUBLICATION_PREFLIGHT",
]

EXPECTED_TOOLS = [
    "tools/android/validate_hero_asset_intake.py",
    "tools/android/p1_visual_source_readiness.py",
    "tools/android/p1_licensed_staging_readiness.py",
    "tools/android/validate_hero_asset_intake_windows.ps1",
    "tools/android/stage_production_candidate_windows.ps1",
    "tools/android/run_p1_staged_candidate_windows.ps1",
    "tools/android/prepare_p1_candidate_device.py",
    "tools/android/device_evidence.py",
    "tools/android/export_p1_device_evidence.py",
    "tools/android/verify_p1_device_review_bundle.py",
    "tools/android/p1_lineage_gate_readiness.py",
    "tools/android/verify_p1_release_publication.py",
]


class P1OperatorReleaseChainContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.chain = json.loads(CHAIN_PATH.read_text(encoding="utf-8"))
        cls.policy = POLICY_PATH.read_text(encoding="utf-8")

    def test_chain_is_authoritative_fail_closed_and_keeps_fixed_register(self):
        self.assertEqual(1, self.chain["schemaVersion"])
        self.assertEqual("P1_AUTHORITATIVE_OPERATOR_CHAIN", self.chain["state"])
        self.assertIs(self.chain["authoritativeForP1"], True)
        self.assertIs(self.chain["genericPublicationVerifierSufficientForP1"], False)
        self.assertIs(self.chain["automationMayPublish"], False)
        self.assertIs(self.chain["automationMayMarkVerified"], False)
        self.assertEqual(65, self.chain["fixedRegisterSize"])

    def test_ordered_stage_ids_and_ordinals_are_exact(self):
        stages = self.chain["orderedStages"]
        self.assertEqual(EXPECTED_STAGE_IDS, [stage["id"] for stage in stages])
        self.assertEqual(list(range(1, len(EXPECTED_STAGE_IDS) + 1)), [stage["order"] for stage in stages])

    def test_every_declared_tool_exists_and_expected_tools_are_not_dropped(self):
        stages = self.chain["orderedStages"]
        declared = [stage["tool"] for stage in stages if stage["tool"] is not None]
        self.assertEqual(EXPECTED_TOOLS, declared)
        for relative in declared:
            path = ROOT / relative
            self.assertTrue(path.is_file(), f"authoritative P1 operator tool is missing: {relative}")

    def test_manual_boundaries_cannot_be_removed(self):
        stages = {stage["id"]: stage for stage in self.chain["orderedStages"]}
        for stage_id in (
            "P1_REVIEW_AND_COMMIT_STAGING_DELTA",
            "P1_PHYSICAL_DEVICE_EVIDENCE",
            "P1_LINEAGE_GATE_READINESS",
            "P1_PUBLICATION_PREFLIGHT",
        ):
            self.assertIs(stages[stage_id]["humanBoundary"], True, stage_id)

        review_requirements = " ".join(stages["P1_REVIEW_AND_COMMIT_STAGING_DELTA"]["requirements"])
        self.assertIn("review", review_requirements.lower())
        self.assertIn("commit", review_requirements.lower())
        self.assertIn("unity_game/Assets", review_requirements)

        device_requirements = " ".join(stages["P1_PHYSICAL_DEVICE_EVIDENCE"]["requirements"])
        self.assertIn("16", device_requirements)
        self.assertIn("no emulator", device_requirements.lower())

        approval_requirements = " ".join(stages["P1_LINEAGE_GATE_READINESS"]["requirements"])
        self.assertIn("UPER-010", approval_requirements)
        self.assertIn("never VERIFIED", approval_requirements)

    def test_publication_preflight_is_last_and_never_performs_publication(self):
        final = self.chain["orderedStages"][-1]
        self.assertEqual("P1_PUBLICATION_PREFLIGHT", final["id"])
        self.assertEqual("tools/android/verify_p1_release_publication.py", final["tool"])
        requirements = " ".join(final["requirements"])
        self.assertIn("publicationPerformed remains false", requirements)
        self.assertIn("verified remains false", requirements)

    def test_policy_makes_p1_override_explicit_and_rejects_generic_shortcut(self):
        self.assertIn("## P1 authoritative override", self.policy)
        self.assertIn("tools/android/p1_operator_release_chain.json", self.policy)
        self.assertIn("tools/android/verify_p1_release_publication.py", self.policy)
        self.assertIn(
            "The generic `tools/android/verify_release_publication.py` is not sufficient to satisfy P1 publication readiness.",
            self.policy,
        )
        self.assertIn("Generic `verify_release_publication.py` alone cannot satisfy this gate.", self.policy)
        self.assertIn("publicationPerformed=false verified=false", self.policy)
        self.assertIn("successful P1 preflight is **not publication**", self.policy)

    def test_staged_candidate_runner_keeps_lineage_verifier_in_front(self):
        runner = (ROOT / "tools" / "android" / "run_p1_staged_candidate_windows.ps1").read_text(encoding="utf-8")
        self.assertIn("verify_p1_staging_lineage_windows.ps1", runner)
        lineage_pos = runner.index("verify_p1_staging_lineage_windows.ps1")
        candidate_pos = runner.index("run_local_candidate_windows.ps1")
        self.assertLess(lineage_pos, candidate_pos)


if __name__ == "__main__":
    unittest.main()
