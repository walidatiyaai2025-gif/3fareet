import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
RANKING_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/RaceRanking.cs"
TEST_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Tests/EditMode/Race/RaceRankingTests.cs"


class RaceRankingAllocationContractTests(unittest.TestCase):
    def test_rank_uses_single_result_list_without_hashset_or_second_ordered_list(self):
        source = RANKING_PATH.read_text(encoding="utf-8")
        start = source.index("public static IReadOnlyList<RankedRaceEntry> Rank(")
        end = source.index("public static int Compare(", start)
        rank = source[start:end]

        self.assertIn("var result = new List<RankedRaceEntry>(racers.Count);", rank)
        self.assertIn("result.Sort(RankedEntryComparison);", rank)
        self.assertIn("result[i] = new RankedRaceEntry(i + 1, result[i].Progress);", rank)
        self.assertIn("StringComparer.Ordinal.Equals", rank)
        self.assertNotIn("new HashSet<", rank)
        self.assertNotIn("var ordered = new List<", rank)
        self.assertEqual(rank.count("new List<"), 1)

    def test_comparison_delegate_is_cached_and_input_order_regression_is_present(self):
        source = RANKING_PATH.read_text(encoding="utf-8")
        tests = TEST_PATH.read_text(encoding="utf-8")

        self.assertIn(
            "private static readonly Comparison<RankedRaceEntry> RankedEntryComparison = CompareEntries;",
            source,
        )
        self.assertIn("private static int CompareEntries", source)
        self.assertIn("RankDoesNotMutateCallerInputOrder", tests)
        self.assertIn("DuplicateRacerIdsAreRejected", tests)


if __name__ == "__main__":
    unittest.main()
