import json
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
CONVERGENCE = REPO_ROOT / "docs/qa/P1_REMEDIATION_CONVERGENCE.json"


class P1RemediationConvergenceContractTests(unittest.TestCase):
    def test_all_eight_remediation_layers_are_present(self):
        manifest = json.loads(CONVERGENCE.read_text(encoding="utf-8"))
        self.assertEqual("8/8", manifest["layerCoverage"])
        self.assertEqual(8, len(manifest["layers"]))
        self.assertEqual(
            {
                "UART-005", "UART-007", "URAC-011", "UART-006",
                "UART-004", "UART-003", "UVEH-012", "UPER-006",
            },
            {layer["taskId"] for layer in manifest["layers"]},
        )
        for layer in manifest["layers"]:
            with self.subTest(task=layer["taskId"]):
                self.assertTrue((REPO_ROOT / layer["requiredPath"]).is_file(), layer)

    def test_convergence_cannot_self_promote_or_change_fixed_register(self):
        manifest = json.loads(CONVERGENCE.read_text(encoding="utf-8"))
        self.assertEqual("BLOCKED", manifest["reviewState"])
        self.assertFalse(manifest["publicationEligible"])
        self.assertFalse(manifest["verified"])
        self.assertEqual(
            {"inReview": 54, "ready": 0, "todo": 0, "blocked": 11, "total": 65},
            manifest["fixedRegister"],
        )

    def test_visual_source_manifests_remain_blocked_and_unverified(self):
        for relative in (
            "docs/assets/02_tracks_environments/cairo_street_kit/ASSET_MANIFEST.json",
            "docs/assets/02_tracks_environments/cairo_track_dressing/ASSET_MANIFEST.json",
            "docs/assets/03_props_architecture/cairo_landmarks/ASSET_MANIFEST.json",
        ):
            payload = json.loads((REPO_ROOT / relative).read_text(encoding="utf-8"))
            with self.subTest(path=relative):
                self.assertEqual("BLOCKED", payload["reviewState"])
                self.assertFalse(payload["runtimeIntegrated"])
                self.assertFalse(payload["runtimeIntegrationVerified"])

        rivals = json.loads(
            (REPO_ROOT / "docs/assets/01_vehicles/rival_cars_production/SOURCE_CANDIDATES.json")
            .read_text(encoding="utf-8")
        )
        self.assertEqual("BLOCKED", rivals["reviewState"])
        self.assertEqual("3/3", rivals["sourceDeliveryProgress"])
        self.assertEqual("0/3", rivals["productionPrefabsBound"])
        self.assertFalse(rivals["licensedUnityImportVerified"])
        self.assertFalse(rivals["runtimeVisualVerified"])

    def test_convergence_contains_fail_closed_hero_handling_and_performance_paths(self):
        hero = (REPO_ROOT / "unity_game/Assets/Afareet/Editor/HeroCarProductionPrefabStager.cs").read_text(encoding="utf-8")
        handling = (REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/VehicleRecoveryPolicy.cs").read_text(encoding="utf-8")
        smoke = (REPO_ROOT / "tools/android/analyze_device_smoke.py").read_text(encoding="utf-8")
        release = (REPO_ROOT / "tools/android/verify_release_with_production_art.py").read_text(encoding="utf-8")

        for forbidden in ("GameObject.CreatePrimitive", "new Mesh(", "RecalculateNormals"):
            self.assertNotIn(forbidden, hero)
        for required in ("StuckDurationSeconds", "IsRecoveryCheckpointAdvanceAllowed"):
            self.assertIn(required, handling)
        for required in ("smoke-cold-start", "smoke-warm-race", "smoke-after-restarts"):
            self.assertIn(required, smoke)
        self.assertIn("performance-tier", release)
        self.assertIn("analyze_device_smoke", release)

    def test_acceptance_pending_preserves_human_and_licensed_gates(self):
        manifest = json.loads(CONVERGENCE.read_text(encoding="utf-8"))
        pending = "\n".join(manifest["acceptancePending"])
        for required in (
            "real authored Afareet King",
            "licensed Unity",
            "Android ARM64",
            "physical-device evidence",
            "UPER-009",
            "UPER-010",
        ):
            self.assertIn(required, pending)


if __name__ == "__main__":
    unittest.main()
