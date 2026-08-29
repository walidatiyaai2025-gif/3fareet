import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Progression/CareerContent.cs"
TESTS = REPO_ROOT / "unity_game/Assets/Afareet/Tests/EditMode/Progression/CareerContentTests.cs"
COMPILE = REPO_ROOT / "tools/android/contracts/CareerFoundationCompile.csproj"
RUNNER = REPO_ROOT / "tools/android/contracts/CareerContentContractRunner.cs"
RUNNER_PROJECT = REPO_ROOT / "tools/android/contracts/CareerContentContractRunner.csproj"


class CareerContentContractTests(unittest.TestCase):
    def test_source_is_pure_and_defines_typed_payloads(self):
        source = SOURCE.read_text(encoding="utf-8")
        self.assertIn("public sealed class CareerObjective", source)
        self.assertIn("public sealed class CareerReward", source)
        self.assertIn("public sealed class CareerNodeDefinition", source)
        self.assertIn("public static class ChapterOneCareerEventContent", source)
        for forbidden in ("UnityEngine", "PlayerPrefs", "System.IO", "HttpClient"):
            self.assertNotIn(forbidden, source)

    def test_exact_legacy_objective_and_reward_formulas_are_retained(self):
        source = SOURCE.read_text(encoding="utf-8")
        self.assertIn('id: $"finish_{node.Id}"', source)
        self.assertIn('description: "Finish the event"', source)
        self.assertIn('id: $"clean_{node.Id}"', source)
        self.assertIn('description: "Finish without restart"', source)
        self.assertIn("coins: checked(250 + index * 100)", source)
        self.assertIn("spirit: checked(5 + index)", source)
        self.assertIn('new CareerReward(unlockVehicleId: "djinn_spirit")', source)

    def test_validation_fails_closed_on_invalid_or_duplicate_content(self):
        source = SOURCE.read_text(encoding="utf-8")
        self.assertIn("Career objective target must be finite and positive.", source)
        self.assertIn("Career reward must contain at least one payload.", source)
        self.assertIn("new HashSet<string>(StringComparer.Ordinal)", source)
        self.assertIn("contains duplicate objective id", source)

    def test_nunit_and_executable_contract_cover_exact_content(self):
        tests = TESTS.read_text(encoding="utf-8")
        runner = RUNNER.read_text(encoding="utf-8")
        for method in (
            "ChapterOneContent_PreservesExactLegacyObjectivesAndRewards",
            "BossContent_AddsExactDjinnSpiritUnlockPayload",
            "ObjectiveAndRewardContracts_FailClosedOnInvalidPayloads",
            "NodeDefinition_RejectsDuplicateObjectivesAndMissingPayloads",
        ):
            self.assertIn(method, tests)
        self.assertIn("Career content behavior contract: PASS", runner)
        self.assertIn('new[] { 250, 350, 450, 550, 650 }', runner)
        self.assertIn('new[] { 5, 6, 7, 8, 9 }', runner)

    def test_shared_compile_gate_and_runner_compile_authoritative_sources(self):
        compile_project = COMPILE.read_text(encoding="utf-8")
        runner_project = RUNNER_PROJECT.read_text(encoding="utf-8")
        self.assertIn("<TargetFramework>netstandard2.1</TargetFramework>", compile_project)
        self.assertIn("CareerContent.cs", compile_project)
        self.assertIn("CareerDefinition.cs", runner_project)
        self.assertIn("ChapterOneCareerContent.cs", runner_project)
        self.assertIn("CareerContent.cs", runner_project)
        self.assertIn("obj/CareerContentContractRunner/", runner_project)


if __name__ == "__main__":
    unittest.main()
