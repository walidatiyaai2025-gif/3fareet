import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class P1RaceVisualStackStagerContractTests(unittest.TestCase):
    def test_operator_menu_reuses_all_visual_stagers_without_promoting_refinement(self):
        path = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1RaceVisualStackStager.cs"
        self.assertTrue(path.is_file())
        self.assertTrue(path.with_suffix(path.suffix + ".meta").is_file())
        text = path.read_text(encoding="utf-8")

        for required in (
            "Afareet/P1/Stage Full Race Visual Stack",
            "P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow",
            "P1ProductionLandmarkAssetStager.StageTrackedSourcesOrThrow",
            "P1ProductionTrackDressingAssetStager.StageTrackedSourcesOrThrow",
            "RivalProductionPrefabStager.StageAndBindAll",
            "HeroCarRefinementCandidateStager.StageCurrentCandidate",
            "AFAREET_P1_VISUAL_STACK_STAGE_BEGIN",
            "AFAREET_P1_VISUAL_STACK_STAGE_STEP_BLOCKED",
            "AFAREET_P1_VISUAL_STACK_STAGE_OK",
            "hero=refinement-candidate",
            "productionGate=false",
            "ownerAcceptance=false",
            "deviceProof=false",
            "p1Verified=false",
        ):
            self.assertIn(required, text)

        for forbidden in (
            "Issue #90",
            "VERIFIED",
            "publication approval",
            "MergePullRequest",
            "GameObject.CreatePrimitive",
            "new Mesh(",
        ):
            self.assertNotIn(forbidden, text)

    def test_staging_is_fail_fast_and_runs_outside_play_mode(self):
        text = (
            REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1RaceVisualStackStager.cs"
        ).read_text(encoding="utf-8")
        self.assertIn("EditorApplication.isPlayingOrWillChangePlaymode", text)
        self.assertIn("throw new InvalidOperationException", text)
        self.assertIn("catch (Exception ex)", text)
        self.assertIn("throw;", text)
        self.assertIn("AssetDatabase.SaveAssets()", text)
        self.assertIn("AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport)", text)


if __name__ == "__main__":
    unittest.main()
