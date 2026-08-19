import json
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SPEC_PATH = REPO_ROOT / "tools/android/p1_production_art_spec.json"
VERIFIER_PATH = REPO_ROOT / "tools/android/verify_p1_production_art.py"
GATE_DOC = REPO_ROOT / "docs/qa/P1_PRODUCTION_ART_GATE.md"
FINGERPRINT_DOC = REPO_ROOT / "docs/qa/P1_PRODUCTION_ART_FINGERPRINTING.md"

RIVAL_SOURCES = [
    "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_01_WedgeCoupe_Production.obj",
    "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_02_FastbackMuscle_Production.obj",
    "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_03_CompactPrototype_Production.obj",
]
RIVAL_PREFABS = [
    "unity_game/Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_01_Production.prefab",
    "unity_game/Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_02_Production.prefab",
    "unity_game/Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_03_Production.prefab",
]


class P1ProductionArtSourcePolicyContractTests(unittest.TestCase):
    def test_spec_binds_uart003_role_and_uart004_exact_exchange_set(self):
        spec = json.loads(SPEC_PATH.read_text(encoding="utf-8"))
        policies = spec["taskArtifactPolicies"]

        hero = policies["UART-003"]
        self.assertEqual(["vehicles"], hero["requiredAuthored3DPathSegments"])
        self.assertEqual(["rivals"], hero["forbiddenAuthored3DPathSegments"])

        rivals = policies["UART-004"]
        self.assertEqual(RIVAL_SOURCES, rivals["exactAuthored3DSourcePaths"])
        self.assertEqual(RIVAL_PREFABS, rivals["requiredRuntimeAssetPaths"])

    def test_spec_rejects_all_integrated_nonproduction_source_families(self):
        spec = json.loads(SPEC_PATH.read_text(encoding="utf-8"))
        self.assertEqual(
            {
                "generated",
                "placeholder",
                "legacyprocedural",
                "preview",
                "refinement",
                "refinementcandidates",
                "blockout",
                "review",
                "reviewpackaging",
            },
            set(spec["forbiddenAuthoredSourcePathSegments"]),
        )

    def test_verifier_enforces_task_policy_before_visual_evidence_acceptance(self):
        text = VERIFIER_PATH.read_text(encoding="utf-8")
        for required in (
            "def _require_task_artifact_policy(",
            'policy.get("exactAuthored3DSourcePaths")',
            'policy.get("requiredRuntimeAssetPaths")',
            'policy.get("requiredAuthored3DPathSegments", [])',
            'policy.get("forbiddenAuthored3DPathSegments", [])',
            "actual_sources == expected_sources",
            "required production runtime asset is missing from evidence",
            "taskArtifactPolicies",
            "taskArtifactPolicyVerified",
        ):
            self.assertIn(required, text)

        self.assertLess(
            text.index("_require_task_artifact_policy(\n            task_id,"),
            text.index("visual_evidence_seen = False"),
        )

    def test_gate_doc_matches_integrated_hero_rival_source_authority(self):
        text = GATE_DOC.read_text(encoding="utf-8")
        for marker in (
            "Generated",
            "Placeholder",
            "LegacyProcedural",
            "Preview",
            "Refinement",
            "RefinementCandidates",
            "Blockout",
            "Review",
            "ReviewPackaging",
            "Vehicles",
            "Rivals",
        ):
            self.assertIn(marker, text)
        for relative in RIVAL_SOURCES + RIVAL_PREFABS:
            self.assertIn(relative, text)
        self.assertIn("verify_release_with_production_art.py", text)
        self.assertIn("Authoritative publication preflight", text)
        self.assertIn("UPER-010", text)
        self.assertIn("verified=false", text)

    def test_fingerprinting_doc_matches_task_policy_and_authoritative_release_sequence(self):
        text = FINGERPRINT_DOC.read_text(encoding="utf-8")
        for relative in RIVAL_SOURCES + RIVAL_PREFABS:
            self.assertIn(relative, text)
        self.assertIn("taskArtifactPolicy=true", text)
        self.assertIn("Vehicles", text)
        self.assertIn("Rivals", text)
        self.assertIn("verify_release_with_production_art.py", text)
        self.assertIn("UPER-006", text)
        self.assertIn("UPER-010", text)
        self.assertIn("verified=false", text)


if __name__ == "__main__":
    unittest.main()
