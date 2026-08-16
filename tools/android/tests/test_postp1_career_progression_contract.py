import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Progression/CareerProgression.cs"
TESTS = REPO_ROOT / "unity_game/Assets/Afareet/Tests/EditMode/Progression/CareerProgressionTests.cs"
COMPILE = REPO_ROOT / "tools/android/contracts/CareerFoundationCompile.csproj"
RUNNER = REPO_ROOT / "tools/android/contracts/CareerProgressionContractRunner.cs"
RUNNER_PROJECT = REPO_ROOT / "tools/android/contracts/CareerProgressionContractRunner.csproj"


class CareerProgressionContractTests(unittest.TestCase):
    def test_source_contract_is_pure_and_versioned(self):
        source = SOURCE.read_text(encoding="utf-8")
        self.assertIn("public const int CurrentVersion = 1;", source)
        self.assertIn("public sealed class CareerProgressionService", source)
        self.assertIn("public CareerProgress CompleteNode", source)
        self.assertIn("public CareerProgress Claim", source)
        self.assertIn("public bool ChapterComplete", source)
        self.assertNotIn("UnityEngine", source)
        self.assertNotIn("PlayerPrefs", source)

    def test_duplicate_completion_and_overflow_fail_safe(self):
        source = SOURCE.read_text(encoding="utf-8")
        self.assertIn("if (progress.IsNodeCompleted(nodeId))\n                return progress;", source)
        self.assertIn("starsEarned < 0 ? 0 : starsEarned > 3 ? 3 : starsEarned", source)
        self.assertIn("checked", source)
        self.assertIn("nextStars = progress.Stars + clampedStars;", source)

    def test_ids_are_defensive_and_deterministic(self):
        source = SOURCE.read_text(encoding="utf-8")
        self.assertIn("new HashSet<string>(StringComparer.Ordinal)", source)
        self.assertIn("ordered.Sort(StringComparer.Ordinal);", source)
        self.assertIn("Career progress ids must be non-blank.", source)

    def test_nunit_regressions_cover_hardening(self):
        tests = TESTS.read_text(encoding="utf-8")
        for method in (
            "CompleteNode_ClampsStarsAndRejectsDuplicateStarFarming",
            "CanEnter_UsesRequiredStarsFromCareerDefinition",
            "Claim_IsIdempotentAndDeterministic",
            "ChapterComplete_RequiresEveryChapterNode",
            "CompleteNode_FailsClosedOnIntegerOverflow",
        ):
            self.assertIn(method, tests)

    def test_existing_netstandard_compile_gate_includes_progression(self):
        project = COMPILE.read_text(encoding="utf-8")
        self.assertIn("<TargetFramework>netstandard2.1</TargetFramework>", project)
        self.assertIn("CareerProgression.cs", project)

    def test_executable_runner_compiles_authoritative_sources(self):
        runner = RUNNER.read_text(encoding="utf-8")
        project = RUNNER_PROJECT.read_text(encoding="utf-8")
        self.assertIn("duplicate completion must not farm stars", runner)
        self.assertIn("star overflow must fail closed", runner)
        self.assertIn("CareerDefinition.cs", project)
        self.assertIn("ChapterOneCareerContent.cs", project)
        self.assertIn("CareerProgression.cs", project)


if __name__ == "__main__":
    unittest.main()
