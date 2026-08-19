import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
RUNTIME = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntime.cs"
EFFECTS = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/PowerUpEffectState.cs"
POLICY = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/AiPowerUpUsagePolicy.cs"


class PowerUpAllocationHotpathContractTests(unittest.TestCase):
    def test_tickall_reuses_zero_snapshot_until_an_effect_expires(self):
        source = RUNTIME.read_text(encoding="utf-8")
        start = source.index("public IReadOnlyList<PowerUpRuntimeTickResult> TickAll")
        end = source.index("public ActivePowerUpEffect GetActiveEffect", start)
        method = source[start:end]
        self.assertIn("private readonly IReadOnlyList<PowerUpRuntimeTickResult> zeroTickResults;", source)
        self.assertIn("List<PowerUpRuntimeTickResult> changedResults = null;", method)
        self.assertIn("? zeroTickResults", method)
        self.assertNotIn("var results = new List<PowerUpRuntimeTickResult>", method)

    def test_live_ai_decision_reads_slots_without_availability_snapshot(self):
        source = RUNTIME.read_text(encoding="utf-8")
        start = source.index("public AiPowerUpExecutionResult ExecuteAiDecision")
        end = source.index("public IReadOnlyList<PowerUpRuntimeTickResult> TickAll", start)
        method = source[start:end]
        self.assertIn("var source = GetRacerOrThrow(sourceRacerId);", method)
        self.assertIn("AiPowerUpUsagePolicy.Decide(", method)
        self.assertIn("IsSlotUsable(source.Inventory[PowerUpKind.EyeShield]", method)
        self.assertNotIn("GetAiAvailability(", method)
        self.assertNotIn("GetInventorySnapshot(", method)

    def test_direct_single_slot_usability_query_is_available(self):
        source = RUNTIME.read_text(encoding="utf-8")
        self.assertIn("public bool IsPowerUpUsable(", source)
        self.assertIn("return IsSlotUsable(racer.Inventory[kind], raceTimeSeconds);", source)

    def test_effect_expiry_has_no_temporary_list_or_sort(self):
        source = EFFECTS.read_text(encoding="utf-8")
        start = source.index("private int RemoveExpired")
        end = source.index("private void EmitPresentation", start)
        method = source[start:end]
        self.assertIn("private static readonly PowerUpKind[] AllPowerUpKinds", source)
        self.assertIn("for (var index = 0; index < AllPowerUpKinds.Length; index++)", method)
        self.assertNotIn("new List<PowerUpKind>", method)
        self.assertNotIn(".Sort(", method)

    def test_public_and_live_ai_policy_share_one_decision_core_without_dictionary(self):
        source = POLICY.read_text(encoding="utf-8")
        self.assertIn("private readonly struct InventoryIndex", source)
        self.assertIn("private struct InventoryIndexBuilder", source)
        self.assertIn("private static AiPowerUpDecision DecideCore(", source)
        self.assertIn("internal static AiPowerUpDecision Decide(", source)
        self.assertNotIn("new Dictionary<PowerUpKind, AiPowerUpAvailability>", source)
        self.assertIn("Duplicate AI power-up inventory entry", source)
        self.assertIn("AI power-up inventory cannot contain null entries", source)


if __name__ == "__main__":
    unittest.main()
