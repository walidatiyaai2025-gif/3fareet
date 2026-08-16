import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class PostP1AiPowerUpUsageContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_policy_is_pure_deterministic_csharp(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/AiPowerUpUsagePolicy.cs")

        self.assertNotIn("using UnityEngine;", source)
        self.assertNotIn("System.Random", source)
        self.assertNotIn("Random.", source)
        self.assertIn("public static AiPowerUpDecision Decide", source)
        self.assertIn("BuildInventoryIndex", source)
        self.assertIn("Duplicate AI power-up inventory entry", source)
        self.assertIn("return AiPowerUpDecision.None();", source)

    def test_decision_priority_contract_is_explicit(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/AiPowerUpUsagePolicy.cs")

        shield = source.index("PowerUpKind.EyeShield")
        asphalt = source.index("PowerUpKind.AsphaltShard", shield)
        nitro = source.index("PowerUpKind.NitroSpirit", asphalt)
        traffic = source.index("PowerUpKind.TrafficCurse", nitro)
        reward = source.index("PowerUpKind.EnchantedPound", traffic)
        self.assertLess(shield, asphalt)
        self.assertLess(asphalt, nitro)
        self.assertLess(nitro, traffic)
        self.assertLess(traffic, reward)

        for required in (
            "snapshot.IncomingHostilePressure && IsUsable(byKind, PowerUpKind.EyeShield)",
            "snapshot.GapFromChaserSeconds <= DefensiveChaserGapSeconds",
            "ShouldUseNitro(snapshot)",
            "snapshot.GapToTargetSeconds <= TrafficCurseMaxTargetGapSeconds",
            "snapshot.NormalizedProgress >= RewardOptimizationMinProgress",
            "HasStableLead(snapshot)",
        ):
            self.assertIn(required, source)

    def test_inventory_and_snapshot_fail_closed(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/AiPowerUpUsagePolicy.cs")

        for required in (
            "public bool IsUsable => Charges > 0 && CooldownRemainingSeconds <= 0d;",
            "if (charges < 0)",
            "if (position <= 0 || position > fieldSize)",
            "ValidateFiniteRange(normalizedProgress, 0d, 1d",
            "ValidateFiniteRange(speedRatio, 0d, 2d",
            "ValidateNonNegative(remainingRaceSeconds",
        ):
            self.assertIn(required, source)

    def test_nunit_regression_covers_acceptance_paths(self):
        tests = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/AiPowerUpUsagePolicyTests.cs")

        for required in (
            "Decide_IsDeterministicForIdenticalInputs",
            "Decide_EyeShieldHasDefensivePrecedence",
            "Decide_DoesNotSelectCooldownOrEmptyInventoryEntry",
            "Decide_TrafficCurseRequiresValidTargetInRange",
            "Decide_NitroHandlesCatchUpAndFinalPush",
            "Decide_AsphaltShardDefendsAgainstCloseChaser",
            "Decide_EnchantedPoundNeverOutranksDefense",
            "Decide_EnchantedPoundRequiresStableLateRaceLead",
            "Decide_RejectsDuplicateInventoryKinds",
        ):
            self.assertIn(required, tests)

    def test_compile_contract_includes_lifecycle_and_ai_policy(self):
        project = self._read("tools/android/contracts/AiPowerUpUsageCompile.csproj")
        self.assertIn("PowerUpEffectState.cs", project)
        self.assertIn("AiPowerUpUsagePolicy.cs", project)
        self.assertIn("netstandard2.1", project)
        self.assertIn("TreatWarningsAsErrors>true", project)

    def test_unity_metadata_is_present(self):
        source_meta = self._read("unity_game/Assets/Afareet/Scripts/Race/AiPowerUpUsagePolicy.cs.meta")
        test_meta = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/AiPowerUpUsagePolicyTests.cs.meta")
        self.assertIn("fileFormatVersion: 2", source_meta)
        self.assertIn("guid:", source_meta)
        self.assertIn("fileFormatVersion: 2", test_meta)
        self.assertIn("guid:", test_meta)


if __name__ == "__main__":
    unittest.main()
