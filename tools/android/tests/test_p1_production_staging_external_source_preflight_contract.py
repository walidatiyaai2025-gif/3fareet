import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
CS_HANDOFF = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionCandidateStagingHandoff.cs"
WINDOWS_HANDOFF = REPO_ROOT / "tools/android/stage_production_candidate_windows.ps1"
DOC = REPO_ROOT / "docs/qa/P1_LICENSED_STAGING_HANDOFF.md"


class P1ProductionStagingExternalSourcePreflightContractTests(unittest.TestCase):
    def test_unity_handoff_validates_hero_and_all_rivals_before_first_staging_mutation(self):
        source = CS_HANDOFF.read_text(encoding="utf-8")
        hero = source.index("ValidateHeroSourceBeforeMutation(heroSourcePath);")
        rivals = source.index("RivalProductionSourcePreflight.ValidateCurrentSourcesOrThrow();")
        external_ok = source.index("AFAREET_P1_STAGING_EXTERNAL_SOURCE_PREFLIGHT_OK")
        world = source.index("P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow();")
        landmarks = source.index("P1ProductionLandmarkAssetStager.StageTrackedSourcesOrThrow();")
        dressing = source.index("P1ProductionTrackDressingAssetStager.StageTrackedSourcesOrThrow();")
        rival_stage = source.index("RivalProductionPrefabStager.StageAndBindAll();")
        hero_stage = source.index("HeroCarProductionPrefabStager.StageAndBind(heroSourcePath);")

        self.assertLess(hero, rivals)
        self.assertLess(rivals, external_ok)
        self.assertLess(external_ok, world)
        self.assertLess(world, landmarks)
        self.assertLess(landmarks, dressing)
        self.assertLess(dressing, rival_stage)
        self.assertLess(rival_stage, hero_stage)
        self.assertIn("trackedInputsValidated=true mutationStarted=false verified=false", source)

    def test_windows_wrapper_requires_tracked_nonempty_vehicle_sources_and_meta_before_unity(self):
        source = WINDOWS_HANDOFF.read_text(encoding="utf-8")
        for required in (
            "function Assert-TrackedNonEmptyFile",
            "ls-files --error-unmatch",
            "$heroMetaRelative = $heroRepoRelative + '.meta'",
            "AFAREET_STAGING_HERO_SOURCE_OK",
            "Rival_01_WedgeCoupe_Production.obj",
            "Rival_02_FastbackMuscle_Production.obj",
            "Rival_03_CompactPrototype_Production.obj",
            "$rivalMetaRelative = $rivalRepoRelative + '.meta'",
            "AFAREET_STAGING_RIVAL_SOURCE_OK",
            "AFAREET_STAGING_EXTERNAL_SOURCE_PREFLIGHT_OK hero=1 rivals=3 sourcesAndMetaTracked=true mutationStarted=false verified=false",
        ):
            self.assertIn(required, source)

        external_ok = source.index("AFAREET_STAGING_EXTERNAL_SOURCE_PREFLIGHT_OK")
        native_hero = source.index("AFAREET_P1_NATIVE_HERO_PREFLIGHT_START")
        unity_launch = source.index("Start-Process -FilePath $UnityPath")
        self.assertLess(external_ok, native_hero)
        self.assertLess(native_hero, unity_launch)

    def test_windows_wrapper_does_not_hide_missing_inputs_behind_fake_authorization_values(self):
        source = WINDOWS_HANDOFF.read_text(encoding="utf-8")
        for required in (
            "[Parameter(Mandatory = $true)]\n    [string]$HandoffPacketSha256",
            "[Parameter(Mandatory = $true)]\n    [string]$NativeHandoffVerificationSha256",
            "[Parameter(Mandatory = $true)]\n    [string]$OperatorChainSha256",
            "Normalize-Sha256 $HandoffPacketSha256 'HandoffPacketSha256'",
            "Normalize-Sha256 $NativeHandoffVerificationSha256 'NativeHandoffVerificationSha256'",
            "Normalize-Sha256 $OperatorChainSha256 'OperatorChainSha256'",
            "authorization fingerprints do not match the native READY-packet authorization",
        ):
            self.assertIn(required, source)
        self.assertNotIn("'0' * 64", source)
        self.assertNotIn("'a' * 64", source)

    def test_operator_doc_matches_full_vehicle_and_authorization_boundary(self):
        text = DOC.read_text(encoding="utf-8")
        for required in (
            "Rival_01_WedgeCoupe_Production.obj",
            "Rival_02_FastbackMuscle_Production.obj",
            "Rival_03_CompactPrototype_Production.obj",
            "The old tracked `Rival_01_WedgeCoupe.obj`",
            "current authoritative READY/operator handoff chain",
            "Do not invent placeholder hashes or reuse values from another commit",
            "Hero production source `.meta`",
            "Rival 01 production OBJ + `.meta`",
            "before the first staging mutation",
            "before opening Unity",
            "AFAREET_STAGING_EXTERNAL_SOURCE_PREFLIGHT_OK",
            "AFAREET_P1_STAGING_EXTERNAL_SOURCE_PREFLIGHT_OK",
            "current Hero refinement candidate and current Rival authored-review OBJ files are explicitly non-production substitutes",
        ):
            self.assertIn(required, text)

        for required_arg in (
            "-HandoffPacketSha256 $handoffPacketSha256",
            "-NativeHandoffVerificationSha256 $nativeHandoffVerificationSha256",
            "-OperatorChainSha256 $operatorChainSha256",
        ):
            self.assertIn(required_arg, text)

    def test_staging_preflight_never_promotes_acceptance(self):
        cs = CS_HANDOFF.read_text(encoding="utf-8")
        ps = WINDOWS_HANDOFF.read_text(encoding="utf-8")
        doc = DOC.read_text(encoding="utf-8")
        self.assertIn("verified=false", cs)
        self.assertIn("verified=false", ps)
        self.assertIn("verified=false", doc)
        self.assertNotIn("ownerAccepted = true", cs)
        self.assertNotIn("publicationEligible = true", cs)
        self.assertNotIn("candidateBuildStarted = true", cs)


if __name__ == "__main__":
    unittest.main()
