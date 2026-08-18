from pathlib import Path
import re
import unittest

REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE_ROOT = REPO_ROOT / "unity_game" / "Assets" / "Afareet" / "Scripts" / "Progression"
TEST_ROOT = REPO_ROOT / "unity_game" / "Assets" / "Afareet" / "Tests" / "EditMode" / "Progression"


class CareerFoundationContractTests(unittest.TestCase):
    def test_domain_is_pure_csharp_and_declares_expected_types(self):
        source = (SOURCE_ROOT / "CareerDefinition.cs").read_text(encoding="utf-8")
        self.assertNotIn("UnityEngine", source)
        for token in (
            "enum CareerRaceMode",
            "Circuit = 0",
            "TimeTrial = 1",
            "Elimination = 2",
            "DriftChallenge = 3",
            "Boss = 4",
            "enum CareerNodeState",
            "sealed class CareerRaceNode",
            "sealed class CareerChapter",
            "sealed class CareerMap",
            "static class CareerDefinitionPolicy",
            "TargetTimeSeconds",
            "TargetDriftScore",
            "BossVehicleId",
        ):
            self.assertIn(token, source)

    def test_mode_specific_validation_remains_fail_closed(self):
        source = (SOURCE_ROOT / "CareerDefinition.cs").read_text(encoding="utf-8")
        self.assertIn("case CareerRaceMode.TimeTrial", source)
        self.assertIn("double.IsNaN(node.TargetTimeSeconds.Value)", source)
        self.assertIn("double.IsInfinity(node.TargetTimeSeconds.Value)", source)
        self.assertIn("case CareerRaceMode.DriftChallenge", source)
        self.assertIn("node.TargetDriftScore.Value <= 0", source)
        self.assertIn("case CareerRaceMode.Boss", source)
        self.assertIn("string.IsNullOrWhiteSpace(node.BossVehicleId)", source)
        self.assertIn("new HashSet<string>(StringComparer.Ordinal)", source)
        self.assertIn("StringComparer.Ordinal.Compare(left.Id, right.Id)", source)

    def test_chapter_one_exact_legacy_contract(self):
        source = (SOURCE_ROOT / "ChapterOneCareerContent.cs").read_text(encoding="utf-8")
        expected = (
            '"chapter_01_cairo_after_dark"',
            '"Cairo After Dark"',
            '"c01_r01"', '"Corniche Run"', 'CareerRaceMode.Circuit', '"cairo_corniche_night"',
            '"c01_r02"', '"Clock of Khan"', 'CareerRaceMode.TimeTrial', '"khan_el_khalili_sprint"', 'targetTimeSeconds: 92d',
            '"c01_r03"', '"Last Car Standing"', 'CareerRaceMode.Elimination', '"ring_road_midnight"',
            '"c01_r04"', '"Spirit Drift"', 'CareerRaceMode.DriftChallenge', '"citadel_drift"', 'targetDriftScore: 12000',
            '"c01_boss"', '"Djinn of the Asphalt"', 'CareerRaceMode.Boss', '"pyramids_spirit_run"', 'bossVehicleId: "djinn_spirit"',
        )
        for token in expected:
            self.assertIn(token, source)
        self.assertEqual(source.count("new CareerRaceNode("), 5)

    def test_navigation_domain_is_pure_deterministic_and_fail_closed(self):
        source = (SOURCE_ROOT / "CareerNavigation.cs").read_text(encoding="utf-8")
        self.assertNotIn("UnityEngine", source)
        for token in (
            "sealed class CareerNavigationNodeSnapshot",
            "sealed class CareerNavigationSnapshot",
            "sealed class CareerNavigationService",
            "CareerNodeState.Completed",
            "CareerNodeState.Locked",
            "CareerNodeState.Available",
            "progress.Stars >= chapter.RequiredStars",
            "new HashSet<string>(StringComparer.Ordinal)",
            "Career navigation contains duplicate node id",
            "Unknown Career navigation node",
            "public CareerNavigationSnapshot Select",
            "public CareerNavigationSnapshot Move",
            "var raw = (long)snapshot.SelectedIndex + delta",
        ):
            self.assertIn(token, source)

    def test_unity_test_and_metadata_contract(self):
        test_source = (TEST_ROOT / "CareerFoundationTests.cs").read_text(encoding="utf-8")
        for test_name in (
            "ChapterOne_ReproducesRetainedLegacyFoundation",
            "ModeSpecificValidation_FailsClosed",
            "ChapterValidation_RejectsDuplicateNodeIds",
            "CareerMap_SortsDeterministicallyAndRejectsDuplicateChapterIds",
            "NodeState_CompletedWinsOtherwiseUsesStarGate",
        ):
            self.assertIn(test_name, test_source)

        navigation_tests = (TEST_ROOT / "CareerNavigationTests.cs").read_text(encoding="utf-8")
        for test_name in (
            "Build_DefaultsToFirstAvailableAndPreservesDeterministicOrder",
            "Build_DerivesCompletedAvailableAndLockedFromAuthoritativeProgress",
            "SelectAndMove_UseStableIdsAndWrapBothDirections",
            "Build_RespectsChapterGateWhileCompletedStateWins",
            "Build_FailsClosedOnDuplicateNodeIdsAcrossChapters",
        ):
            self.assertIn(test_name, navigation_tests)

        prod_asmdef = (SOURCE_ROOT / "Afareet.Progression.asmdef").read_text(encoding="utf-8")
        test_asmdef = (TEST_ROOT / "Afareet.ProgressionEditModeTests.asmdef").read_text(encoding="utf-8")
        self.assertIn('"name": "Afareet.Progression"', prod_asmdef)
        self.assertIn('"references": []', prod_asmdef)
        self.assertIn('"Afareet.Progression"', test_asmdef)
        self.assertIn('"TestAssemblies"', test_asmdef)
        self.assertIn('"Editor"', test_asmdef)

        for path in (
            SOURCE_ROOT / "CareerDefinition.cs.meta",
            SOURCE_ROOT / "ChapterOneCareerContent.cs.meta",
            SOURCE_ROOT / "CareerNavigation.cs.meta",
            SOURCE_ROOT / "Afareet.Progression.asmdef.meta",
            TEST_ROOT / "CareerFoundationTests.cs.meta",
            TEST_ROOT / "CareerNavigationTests.cs.meta",
            TEST_ROOT / "Afareet.ProgressionEditModeTests.asmdef.meta",
        ):
            text = path.read_text(encoding="utf-8")
            self.assertIn("fileFormatVersion: 2", text)
            self.assertRegex(text, r"guid: [0-9a-f]{32}")

    def test_dotnet_and_workflow_contract(self):
        compile_project = (REPO_ROOT / "tools" / "android" / "contracts" / "CareerFoundationCompile.csproj").read_text(encoding="utf-8")
        runner_project = (REPO_ROOT / "tools" / "android" / "contracts" / "CareerFoundationContractRunner.csproj").read_text(encoding="utf-8")
        runner = (REPO_ROOT / "tools" / "android" / "contracts" / "CareerFoundationContractRunner.cs").read_text(encoding="utf-8")
        workflow = (REPO_ROOT / ".github" / "workflows" / "postp1-career-foundation-contract.yml").read_text(encoding="utf-8")

        self.assertIn("<TargetFramework>netstandard2.1</TargetFramework>", compile_project)
        self.assertIn("CareerDefinition.cs", compile_project)
        self.assertIn("ChapterOneCareerContent.cs", compile_project)
        self.assertIn("CareerNavigation.cs", compile_project)
        self.assertIn("<TargetFramework>net8.0</TargetFramework>", runner_project)
        self.assertIn("Career foundation behavior contract: PASS", runner)
        self.assertIn("NavigationContract();", runner)
        self.assertIn("dotnet build tools/android/contracts/CareerFoundationCompile.csproj", workflow)
        self.assertIn("dotnet run --project tools/android/contracts/CareerFoundationContractRunner.csproj", workflow)
        self.assertIn("python3 tools/android/tests/test_postp1_career_foundation_contract.py", workflow)


if __name__ == "__main__":
    unittest.main()