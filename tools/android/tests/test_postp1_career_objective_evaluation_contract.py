import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Progression/CareerObjectiveEvaluation.cs"
TESTS = REPO_ROOT / "unity_game/Assets/Afareet/Tests/EditMode/Progression/CareerObjectiveEvaluationTests.cs"
COMPILE = REPO_ROOT / "tools/android/contracts/CareerFoundationCompile.csproj"
RUNNER = REPO_ROOT / "tools/android/contracts/CareerObjectiveEvaluationContractRunner.cs"
RUNNER_PROJECT = REPO_ROOT / "tools/android/contracts/CareerObjectiveEvaluationContractRunner.csproj"


class CareerObjectiveEvaluationContractTests(unittest.TestCase):
    def test_source_is_pure_and_caller_supplies_restart_count(self):
        source = SOURCE.read_text(encoding="utf-8")
        self.assertIn("public sealed class CareerEventOutcome", source)
        self.assertIn("public int RestartCount", source)
        self.assertIn("restartCount < 0", source)
        self.assertNotIn("UnityEngine", source)
        self.assertNotIn("Afareet.Race", source)
        self.assertNotIn("PlayerPrefs", source)
        self.assertNotIn("System.IO", source)

    def test_finish_and_clean_semantics_are_explicit_and_fail_closed(self):
        source = SOURCE.read_text(encoding="utf-8")
        self.assertIn('var finishId = $"finish_{definition.Node.Id}";', source)
        self.assertIn('var cleanId = $"clean_{definition.Node.Id}";', source)
        self.assertIn("outcome.Finished ? 1d : 0d", source)
        self.assertIn("outcome.Finished && outcome.RestartCount == 0 ? 1d : 0d", source)
        self.assertIn("private const double BinaryObjectiveTarget = 1d;", source)
        self.assertIn("uses unsupported non-binary target", source)
        self.assertIn("is not supported for node", source)

    def test_evaluation_output_is_ordered_and_immutable(self):
        source = SOURCE.read_text(encoding="utf-8")
        self.assertIn("new List<CareerObjectiveEvaluationEntry>(definition.Objectives.Count)", source)
        self.assertIn("for (var index = 0; index < definition.Objectives.Count; index++)", source)
        self.assertIn("this.entries = list.AsReadOnly();", source)
        self.assertIn("public int CompletedCount", source)
        self.assertIn("public bool AllCompleted", source)

    def test_nunit_regressions_cover_runtime_semantics_and_guards(self):
        tests = TESTS.read_text(encoding="utf-8")
        for method in (
            "FinishedEvent_CompletesFinishObjective",
            "CleanFinish_WithZeroRestarts_CompletesBothObjectives",
            "RestartedFinish_BlocksCleanObjectiveOnly",
            "UnfinishedEvent_CompletesNeitherObjective",
            "Evaluation_PreservesDefinitionOrder",
            "UnknownObjective_FailsClosed",
            "NonBinaryTarget_FailsClosed",
            "NegativeRestartCount_FailsClosed",
        ):
            self.assertIn(method, tests)

    def test_shared_compile_gate_includes_evaluator(self):
        project = COMPILE.read_text(encoding="utf-8")
        self.assertIn("<TargetFramework>netstandard2.1</TargetFramework>", project)
        self.assertIn("CareerObjectiveEvaluation.cs", project)

    def test_executable_runner_compiles_authoritative_sources(self):
        runner = RUNNER.read_text(encoding="utf-8")
        project = RUNNER_PROJECT.read_text(encoding="utf-8")
        self.assertIn("restart must block clean objective only", runner)
        self.assertIn("unknown objective must fail closed", runner)
        self.assertIn("non-binary target must fail closed", runner)
        self.assertIn("CareerDefinition.cs", project)
        self.assertIn("ChapterOneCareerContent.cs", project)
        self.assertIn("CareerContent.cs", project)
        self.assertIn("CareerObjectiveEvaluation.cs", project)
        self.assertIn("BaseIntermediateOutputPath", project)


if __name__ == "__main__":
    unittest.main()
