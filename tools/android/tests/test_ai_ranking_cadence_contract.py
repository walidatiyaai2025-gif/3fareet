import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
DIRECTOR_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/RaceDirector.cs"


class AiRankingCadenceContractTests(unittest.TestCase):
    def test_bound_ai_uses_batch_snapshot_with_fresh_external_fallback(self):
        source = DIRECTOR_PATH.read_text(encoding="utf-8")
        start = source.index("internal AiPowerUpExecutionResult ExecuteBoundAiPowerUp")
        end = source.index("private void FixedUpdate()", start)
        method = source[start:end]

        self.assertIn("private IReadOnlyList<RankedRaceEntry> aiDecisionRankedSnapshot;", source)
        self.assertIn("var ranked = aiDecisionRankedSnapshot ?? BuildRankedRace();", method)
        self.assertNotIn("var ranked = BuildRankedRace();", method)

    def test_ai_cadence_builds_ranking_lazily_once_and_always_clears_it(self):
        source = DIRECTOR_PATH.read_text(encoding="utf-8")
        start = source.index("private void FixedUpdate()")
        end = source.index("private void PrepareRacer", start)
        fixed_update = source[start:end]

        self.assertIn("if (ai == null || !ai.isActiveAndEnabled) continue;", fixed_update)
        self.assertIn("aiDecisionRankedSnapshot ??= BuildRankedRace();", fixed_update)
        self.assertEqual(fixed_update.count("BuildRankedRace()"), 1)
        self.assertIn("var execution = ai.EvaluateBoundPowerUpDecision();", fixed_update)
        self.assertIn("try", fixed_update)
        self.assertIn("finally", fixed_update)
        self.assertGreaterEqual(fixed_update.count("aiDecisionRankedSnapshot = null;"), 2)

    def test_lifecycle_resets_cadence_snapshot(self):
        source = DIRECTOR_PATH.read_text(encoding="utf-8")
        for marker in (
            "nextPowerUpDecisionRaceTime = 0d;\n            aiDecisionRankedSnapshot = null;",
            "private void OnDestroy()\n        {\n            aiDecisionRankedSnapshot = null;",
        ):
            self.assertIn(marker, source)


if __name__ == "__main__":
    unittest.main()
