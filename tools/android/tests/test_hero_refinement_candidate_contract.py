import json
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]

class HeroRefinementCandidateContractTests(unittest.TestCase):
    def text(self, relative):
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_exact_external_intake_manifest_is_non_production(self):
        manifest_path = REPO_ROOT / "tools/android/hero_refinement_candidate_manifest.json"
        data = json.loads(manifest_path.read_text(encoding="utf-8"))
        self.assertEqual(data["classification"], "REFINEMENT_CANDIDATE")
        self.assertEqual(data["sourceFileName"], "AfareetKing_Hero.fbx")
        self.assertEqual(data["sha256"], "97b02c87118c451d068c881fc551787d6e468ec8002cce7802db62258cc4cda2")
        self.assertEqual(data["sizeBytes"], 1475244)
        self.assertFalse(data["productionGate"])
        self.assertFalse(data["visualAcceptance"])
        self.assertIn("RefinementCandidates/AfareetKing_Hero.fbx", data["unityDestination"].replace("\\", "/"))

        intake = self.text("tools/android/import_hero_refinement_candidate_windows.ps1")
        self.assertIn(data["sha256"], intake)
        self.assertIn("Get-FileHash", intake)
        self.assertIn("productionGate=false", intake)

    def test_refinement_intake_and_staged_prefab_are_local_only(self):
        ignore = self.text(".gitignore")
        for required in (
            "unity_game/Assets/Afareet/ArtSource/Vehicles/RefinementCandidates/",
            "unity_game/Assets/Afareet/ArtSource/Vehicles/RefinementCandidates.meta",
            "unity_game/Assets/Afareet/Resources/Art/Vehicles/HeroCar/Refinement/",
            "unity_game/Assets/Afareet/Resources/Art/Vehicles/HeroCar/Refinement.meta",
        ):
            self.assertIn(required, ignore)

        self.assertIn("local refinement preview only", ignore)
        self.assertIn("cannot be mistaken for the final externally-authored production Hero source", ignore)

    def test_production_provenance_rejects_refinement_paths(self):
        metadata = self.text("unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarProductionAssetMetadata.cs")
        binder = self.text("unity_game/Assets/Afareet/Editor/HeroCarProductionSourceBinder.cs")
        gate = self.text("unity_game/Assets/Afareet/Editor/HeroCarProductionBuildPreprocessor.cs")

        for forbidden_segment in ("/Generated/", "/Preview/", "/Refinement/", "/RefinementCandidates/", "/Blockout/"):
            self.assertIn(forbidden_segment, metadata)

        self.assertIn("IsNonProductionSourcePath(sourcePath)", binder)
        self.assertIn("IsNonProductionSourcePath(sourcePath)", gate)
        self.assertIn("refinement-candidate-must-not-ship", gate)

    def test_refinement_is_editor_or_experimental_only(self):
        visual = self.text("unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarProductionVisual.cs")
        installer = self.text("unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarProductionVisualInstaller.cs")
        marker = self.text("unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarRefinementCandidateMarker.cs")

        self.assertIn("#if !UNITY_EDITOR && !AFAREET_EXPERIMENTAL_APK", visual)
        self.assertIn("TryAttachRefinementCandidate", installer)
        self.assertIn("#if UNITY_EDITOR || AFAREET_EXPERIMENTAL_APK", installer)
        self.assertLess(installer.index("HeroCarProductionVisual.TryAttach(hero.transform)"),
                        installer.index("HeroCarProductionVisual.TryAttachRefinementCandidate(hero.transform)"))
        self.assertIn("CanSatisfyProductionGate => false", marker)
        self.assertIn('ExpectedClassification = "REFINEMENT_CANDIDATE"', marker)

    def test_stager_builds_three_lods_without_relaxing_production_validator(self):
        stager = self.text("unity_game/Assets/Afareet/Editor/HeroCarRefinementCandidateStager.cs")
        visual = self.text("unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarProductionVisual.cs")

        self.assertIn("new LOD(HeroCarLodPolicy.Lod0Transition", stager)
        self.assertIn("new LOD(HeroCarLodPolicy.Lod1Transition", stager)
        self.assertIn("new LOD(HeroCarLodPolicy.Lod2Transition", stager)
        self.assertIn("productionGate=false", stager)
        self.assertIn("renderers.Length != 1", visual,
                      "Final production validator must keep its one-renderer-per-LOD contract.")
        self.assertIn("ValidateRefinementCandidatePrefab", visual)

if __name__ == "__main__":
    unittest.main()
