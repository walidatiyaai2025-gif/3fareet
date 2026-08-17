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
            "HeroCarRefinementCandidateStager.ValidateCurrentCandidateSourceOrThrow",
            "P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow",
            "P1ProductionLandmarkAssetStager.StageTrackedSourcesOrThrow",
            "P1ProductionTrackDressingAssetStager.StageTrackedSourcesOrThrow",
            "RivalProductionPrefabStager.StageAndBindAll",
            "HeroCarRefinementCandidateStager.StageCurrentCandidate",
            "AFAREET_P1_VISUAL_STACK_PREFLIGHT_BEGIN",
            "AFAREET_P1_VISUAL_STACK_PREFLIGHT_OK",
            "AFAREET_P1_VISUAL_STACK_PREFLIGHT_BLOCKED",
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

    def test_hero_intake_is_preflighted_before_any_stage_mutation(self):
        orchestrator = (
            REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1RaceVisualStackStager.cs"
        ).read_text(encoding="utf-8")
        preflight_call = orchestrator.index(
            "HeroCarRefinementCandidateStager.ValidateCurrentCandidateSourceOrThrow"
        )
        first_stage = orchestrator.index(
            'RunStage("UART-005 Cairo production sources"'
        )
        self.assertLess(preflight_call, first_stage)

        hero_stager = (
            REPO_ROOT / "unity_game/Assets/Afareet/Editor/HeroCarRefinementCandidateStager.cs"
        ).read_text(encoding="utf-8")
        for required in (
            "public static void ValidateCurrentCandidateSourceOrThrow()",
            "HeroCarLodPolicy.RefinementCandidateSourcePath",
            "HeroCarProductionAssetMetadata.IsSupportedExternalModelSource",
            "HeroCarProductionAssetMetadata.IsNonProductionSourcePath",
            "AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath)",
            "Sha256ForProjectAsset(sourcePath)",
            "ExpectedSourceSha256",
            "import_hero_refinement_candidate_windows.ps1",
            "AFAREET_HERO_REFINEMENT_PREFLIGHT_OK",
            "classification=REFINEMENT_CANDIDATE",
            "productionGate=false",
        ):
            self.assertIn(required, hero_stager)

        method_start = hero_stager.index(
            "public static void ValidateCurrentCandidateSourceOrThrow()"
        )
        method_end = hero_stager.index("private static int ClassifyLod", method_start)
        preflight_body = hero_stager[method_start:method_end]
        for forbidden in (
            "PrefabUtility.SaveAsPrefabAsset",
            "AssetDatabase.SaveAssets",
            "AssetDatabase.CreateFolder",
            "GameObject(",
        ):
            self.assertNotIn(forbidden, preflight_body)

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
