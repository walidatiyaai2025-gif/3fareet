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
            'private const string GitShaArgument = "-afareetGitSha"',
            'private const string HandoffPacketSha256Argument = "-afareetHandoffPacketSha256"',
            'private const string NativeHandoffVerificationSha256Argument = "-afareetNativeHandoffVerificationSha256"',
            'private const string OperatorChainSha256Argument = "-afareetOperatorChainSha256"',
            "string handoffPacketSha256",
            "string nativeHandoffVerificationSha256",
            "string operatorChainSha256",
            "gitSha = NormalizeGitSha(gitSha)",
            'NormalizeSha256(handoffPacketSha256, "handoff packet")',
            'NormalizeSha256(nativeHandoffVerificationSha256, "native handoff verification")',
            'NormalizeSha256(operatorChainSha256, "operator chain")',
            "ValidateHeroSourceBeforeMutation(heroSourcePath)",
            "RivalProductionSourcePreflight.ValidateCurrentSourcesOrThrow()",
            "P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow()",
            "P1ProductionLandmarkAssetStager.StageTrackedSourcesOrThrow()",
            "P1ProductionTrackDressingAssetStager.StageTrackedSourcesOrThrow()",
            "RivalProductionPrefabStager.StageAndBindAll()",
            "HeroCarProductionPrefabStager.StageAndBind(heroSourcePath)",
            "BuildTaskEvidence(heroSourcePath)",
            "schemaVersion = 3",
            "authorizationSourceGitSha",
            "handoffPacketSha256",
            "nativeHandoffVerificationSha256",
            "operatorChainSha256",
            '"UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"',
            "LICENSED_UNITY_STAGE_AND_BIND_OK",
            "LICENSED_UNITY_IMPORT_STAGE_OK",
            "LICENSED_UNITY_TRACKED_LAYOUT_IMPORT_OK",
            "RequiredGuid(heroSourcePath",
            "RivalProductionAssetMetadata",
            "VerticalSliceLayoutPath",
            "STAGED_FOR_COMMIT_NOT_CANDIDATE",
            "trackedCommitRequired = true",
            "candidateBuildStarted = false",
            "publicationEligible = false",
            "runtimeVerified = false",
            "ownerAccepted = false",
            "verified = false",
            "AFAREET_P1_STAGING_HANDOFF_OK",
        ):
            self.assertIn(required, text)

        self.assertLess(
            text.index("RivalProductionSourcePreflight.ValidateCurrentSourcesOrThrow()"),
            text.index("P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow()"),
        )

        for forbidden in (
            "AfareetBuild.BuildAndroid",
            "BuildPipeline.BuildPlayer",
            "run_local_candidate_windows.ps1",
            'reviewState = "ACCEPTED"',
            'verified = true',
            'runtimeVerified = true',
            'ownerAccepted = true',
            'publicationEligible = true',
        ):
            self.assertNotIn(forbidden, text)

    def test_staging_report_covers_exact_six_visual_runtime_tasks(self):
        text = (EDITOR / "P1ProductionCandidateStagingHandoff.cs").read_text(encoding="utf-8")
        for task_id in ("UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"):
            self.assertIn(f'"{task_id}"', text)
        self.assertNotIn('"UPER-009"', text)
        self.assertNotIn('"UPER-010"', text)
        self.assertIn("taskEvidence.Length != 6", text)
        self.assertIn("P1 staging handoff report requires evidence for exactly six visual/runtime tasks.", text)

    def test_existing_hero_and_rival_stagers_expose_shared_internal_entries(self):
        hero = (EDITOR / "HeroCarProductionPrefabStager.cs").read_text(encoding="utf-8")
        rivals = (EDITOR / "RivalProductionPrefabStager.cs").read_text(encoding="utf-8")
        self.assertIn("internal static void StageAndBind(string sourcePath)", hero)
        self.assertIn("internal static void StageAndBindAll()", rivals)
        self.assertNotIn("GameObject.CreatePrimitive", hero)
        self.assertNotIn("new Mesh(", hero)
        self.assertNotIn("GameObject.CreatePrimitive", rivals)
        self.assertNotIn("new Mesh(", rivals)

    def test_windows_handoff_requires_authorization_clean_tracked_vehicle_sources_and_never_commits_or_builds(self):
        text = (TOOLS / "stage_production_candidate_windows.ps1").read_text(encoding="utf-8")

        for required in (
            "[string]$HandoffPacketSha256",
            "[string]$NativeHandoffVerificationSha256",
            "[string]$OperatorChainSha256",
            "Staging handoff requires a clean Git tree.",
            "function Assert-TrackedNonEmptyFile",
            "ls-files --error-unmatch -- $normalized",
            "must already be tracked in the clean starting commit before licensed staging",
            "$heroBytes = Assert-TrackedNonEmptyFile $heroRepoRelative 'Hero production source'",
            "$heroMetaBytes = Assert-TrackedNonEmptyFile $heroMetaRelative 'Hero production source Unity metadata'",
            "Rival_01_WedgeCoupe_Production.obj",
            "Rival_02_FastbackMuscle_Production.obj",
            "Rival_03_CompactPrototype_Production.obj",
            "$rivalBytes = Assert-TrackedNonEmptyFile",
            "$rivalMetaBytes = Assert-TrackedNonEmptyFile",
            "AFAREET_STAGING_EXTERNAL_SOURCE_PREFLIGHT_OK",
            "validate_hero_asset_intake_windows.ps1",
            "AFAREET_P1_NATIVE_HERO_PREFLIGHT_START",
            "AFAREET_P1_NATIVE_HERO_PREFLIGHT_OK",
            "Native intake must never self-assert verified or production-art approval.",
            "READY_FOR_LICENSED_UNITY_IMPORT",
            "UNITY_INSPECTION_REQUIRED",
            "Afareet.Editor.P1ProductionCandidateStagingHandoff.StageForCommit",
            "-afareetHeroSource",
            "-afareetGitSha",
            "-afareetHandoffPacketSha256",
            "-afareetNativeHandoffVerificationSha256",
            "-afareetOperatorChainSha256",
            "schemaVersion -ne 3",
            "handoffReport.gitSha -ne $gitSha",
            "handoffReport.heroSource -ne $HeroSource",
            "handoffReport.handoffPacketSha256",
            "handoffReport.nativeHandoffVerificationSha256",
            "handoffReport.operatorChainSha256",
            "handoffReport.authorizationSourceGitSha",
            "AFAREET_P1_STAGING_REPORT_BINDING_OK",
            "runtimeVerified=false",
            "ownerAccepted=false",
            "LICENSED_UNITY_TRACKED_LAYOUT_IMPORT_OK",
            "AFAREET_P1_STAGING_HANDOFF_OK",
            "AFAREET_P1_STAGING_COMMIT_REQUIRED",
            "unity_game/Assets/",
            "Do not commit blindly",
            "run_p1_staged_candidate_windows.ps1 from the new clean SHA",
        ):
            self.assertIn(required, text)

        for task_id in ("UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"):
            self.assertIn(f"'{task_id}'", text)

        for forbidden in (
            "git add",
            "git commit",
            "Afareet.Editor.AfareetBuild.BuildAndroid",
            "& $buildScript",
            "& $testScript",
            "-AllowDirty",
        ):
            self.assertNotIn(forbidden, text)

        external_preflight = text.index("AFAREET_STAGING_EXTERNAL_SOURCE_PREFLIGHT_OK")
        native_preflight = text.index("AFAREET_P1_NATIVE_HERO_PREFLIGHT_START")
        unity_lookup = text.index("$defaultUnity = Join-Path")
        unity_start = text.index("Start-Process -FilePath $UnityPath")
        report_binding = text.index("AFAREET_P1_STAGING_REPORT_BINDING_OK")
        status_capture = text.index("status --porcelain --untracked-files=all", unity_start)
        self.assertLess(external_preflight, native_preflight)
        self.assertLess(native_preflight, unity_lookup)
        self.assertLess(native_preflight, unity_start)
        self.assertLess(unity_start, report_binding)
        self.assertLess(report_binding, status_capture)

    def test_authoritative_wrapper_forwards_authorization_before_low_level_staging(self):
        text = (TOOLS / "run_p1_licensed_staging_windows.ps1").read_text(encoding="utf-8")
        for marker in (
            "verification.packetSha256",
            "verification.operatorChainSha256",
            "Get-FileHash -Algorithm SHA256 -LiteralPath $verifyOutput",
            "HandoffPacketSha256 = $handoffPacketSha256",
            "NativeHandoffVerificationSha256 = $nativeVerificationSha256",
            "OperatorChainSha256 = $operatorChainSha256",
        ):
            self.assertIn(marker, text)
        self.assertLess(text.index("& $verifyScript"), text.index("& $stageScript"))

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

        for script in (
            TOOLS / "run_p1_licensed_staging_windows.ps1",
            TOOLS / "stage_production_candidate_windows.ps1",
            TOOLS / "validate_hero_asset_intake_windows.ps1",
        ):
            command = (
                "$tokens=$null; $errors=$null; "
                f"[System.Management.Automation.Language.Parser]::ParseFile('{script.as_posix()}', "
                "[ref]$tokens, [ref]$errors) | Out-Null; "
                "if ($errors.Count -gt 0) { $errors | ForEach-Object { Write-Error $_.Message }; exit 1 }"
            )
            result = subprocess.run([pwsh, "-NoProfile", "-Command", command, capture_output=True, text=True)
            self.assertEqual(0, result.returncode, msg=f"{script.name}\n{result.stdout}{result.stderr}")


if __name__ == "__main__":
    unittest.main()
