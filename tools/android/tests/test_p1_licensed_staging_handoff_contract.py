import shutil
import subprocess
import unittest
from pathlib import Path


REPO = Path(__file__).resolve().parents[3]
EDITOR = REPO / "unity_game" / "Assets" / "Afareet" / "Editor"
TOOLS = REPO / "tools" / "android"


class P1LicensedStagingHandoffContractTests(unittest.TestCase):
    def test_unity_batch_handoff_reuses_existing_stagers_and_stops_before_candidate(self):
        text = (EDITOR / "P1ProductionCandidateStagingHandoff.cs").read_text(encoding="utf-8")

        for required in (
            "public static void StageForCommit()",
            'private const string HeroSourceArgument = "-afareetHeroSource"',
            "ValidateHeroSourceBeforeMutation(heroSourcePath)",
            "P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow()",
            "P1ProductionLandmarkAssetStager.StageTrackedSourcesOrThrow()",
            "P1ProductionTrackDressingAssetStager.StageTrackedSourcesOrThrow()",
            "RivalProductionPrefabStager.StageAndBindAll()",
            "HeroCarProductionPrefabStager.StageAndBind(heroSourcePath)",
            "STAGED_FOR_COMMIT_NOT_CANDIDATE",
            "trackedCommitRequired = true",
            "candidateBuildStarted = false",
            "publicationEligible = false",
            "verified = false",
            "AFAREET_P1_STAGING_HANDOFF_OK",
        ):
            self.assertIn(required, text)

        for forbidden in (
            "AfareetBuild.BuildAndroid",
            "BuildPipeline.BuildPlayer",
            "run_local_candidate_windows.ps1",
            'reviewState = "ACCEPTED"',
            'verified = true',
        ):
            self.assertNotIn(forbidden, text)

    def test_existing_hero_and_rival_stagers_expose_shared_internal_entries(self):
        hero = (EDITOR / "HeroCarProductionPrefabStager.cs").read_text(encoding="utf-8")
        rivals = (EDITOR / "RivalProductionPrefabStager.cs").read_text(encoding="utf-8")
        self.assertIn("internal static void StageAndBind(string sourcePath)", hero)
        self.assertIn("internal static void StageAndBindAll()", rivals)
        self.assertNotIn("GameObject.CreatePrimitive", hero)
        self.assertNotIn("new Mesh(", hero)
        self.assertNotIn("GameObject.CreatePrimitive", rivals)
        self.assertNotIn("new Mesh(", rivals)

    def test_windows_handoff_requires_clean_tracked_hero_and_never_commits_or_builds(self):
        text = (TOOLS / "stage_production_candidate_windows.ps1").read_text(encoding="utf-8")

        for required in (
            "Staging handoff requires a clean Git tree.",
            "ls-files --error-unmatch",
            "Hero source must already be tracked in the clean starting commit",
            "Afareet.Editor.P1ProductionCandidateStagingHandoff.StageForCommit",
            "-afareetHeroSource",
            "AFAREET_P1_STAGING_HANDOFF_OK",
            "AFAREET_P1_STAGING_COMMIT_REQUIRED",
            "unity_game/Assets/",
            "Do not commit blindly",
            "run_local_candidate_windows.ps1 from the new clean SHA",
        ):
            self.assertIn(required, text)

        for forbidden in (
            "git add",
            "git commit",
            "Afareet.Editor.AfareetBuild.BuildAndroid",
            "& $buildScript",
            "& $testScript",
            "-AllowDirty",
        ):
            self.assertNotIn(forbidden, text)

    def test_exact_candidate_runner_remains_separate_and_clean_sha_locked(self):
        candidate = (TOOLS / "run_local_candidate_windows.ps1").read_text(encoding="utf-8")
        self.assertIn("Candidate orchestration requires a clean Git working tree before Unity starts.", candidate)
        self.assertIn('& $testScript @sharedParams', candidate)
        self.assertIn('& $buildScript @sharedParams', candidate)
        self.assertNotIn("stage_production_candidate_windows.ps1", candidate)
        self.assertNotIn("P1ProductionCandidateStagingHandoff.StageForCommit", candidate)

    def test_build_time_ignored_resource_staging_remains_deterministic(self):
        build = (EDITOR / "AfareetBuild.cs").read_text(encoding="utf-8")
        landmarks = (EDITOR / "P1ProductionLandmarkBuildPreprocessor.cs").read_text(encoding="utf-8")
        dressing = (EDITOR / "P1ProductionTrackDressingBuildPreprocessor.cs").read_text(encoding="utf-8")
        self.assertIn("P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow()", build)
        self.assertIn("P1ProductionLandmarkAssetStager.StageTrackedSourcesOrThrow()", landmarks)
        self.assertIn("P1ProductionTrackDressingAssetStager.StageTrackedSourcesOrThrow()", dressing)

    def test_windows_handoff_powershell_parses_when_pwsh_is_available(self):
        pwsh = shutil.which("pwsh")
        if not pwsh:
            self.skipTest("pwsh is not installed in this environment")

        script = TOOLS / "stage_production_candidate_windows.ps1"
        command = (
            "$tokens=$null; $errors=$null; "
            f"[System.Management.Automation.Language.Parser]::ParseFile('{script.as_posix()}', "
            "[ref]$tokens, [ref]$errors) | Out-Null; "
            "if ($errors.Count -gt 0) { $errors | ForEach-Object { Write-Error $_.Message }; exit 1 }"
        )
        result = subprocess.run([pwsh, "-NoProfile", "-Command", command], capture_output=True, text=True)
        self.assertEqual(0, result.returncode, msg=result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
