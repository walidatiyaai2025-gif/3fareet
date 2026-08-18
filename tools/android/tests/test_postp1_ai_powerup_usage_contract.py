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
        self.assertIn("internal static AiPowerUpDecision Decide", source)
        self.assertIn("private static AiPowerUpDecision DecideCore", source)
        self.assertIn("BuildInventoryIndex", source)
        self.assertIn("Duplicate AI power-up inventory entry", source)
        self.assertIn("return AiPowerUpDecision.None();", source)

    def test_public_and_live_paths_share_one_decision_priority_core(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/AiPowerUpUsagePolicy.cs")
        public_start = source.index("public static AiPowerUpDecision Decide(")
        live_start = source.index("internal static AiPowerUpDecision Decide(", public_start)
        core_start = source.index("private static AiPowerUpDecision DecideCore(", live_start)
        inventory_start = source.index("private static InventoryIndex BuildInventoryIndex", core_start)

        public_method = source[public_start:live_start]
        live_method = source[live_start:core_start]
        core = source[core_start:inventory_start]

        self.assertIn("var byKind = BuildInventoryIndex(inventory);", public_method)
        self.assertIn("return DecideCore(", public_method)
        for kind in (
            "PowerUpKind.AsphaltShard",
            "PowerUpKind.NitroSpirit",
            "PowerUpKind.TrafficCurse",
            "PowerUpKind.EnchantedPound",
            "PowerUpKind.EyeShield",
        ):
            self.assertIn(f"IsUsable(byKind, {kind})", public_method)

        self.assertIn("return DecideCore(", live_method)
        self.assertNotIn("new AiPowerUpAvailability", live_method)
        self.assertNotIn("new List<", live_method)
        self.assertNotIn("BuildInventoryIndex", live_method)

        for required in (
            "snapshot.IncomingHostilePressure && eyeShieldUsable",
            "snapshot.GapFromChaserSeconds <= DefensiveChaserGapSeconds",
            "asphaltShardUsable",
            "nitroSpiritUsable",
            "ShouldUseNitro(snapshot)",
            "snapshot.GapToTargetSeconds <= TrafficCurseMaxTargetGapSeconds",
            "trafficCurseUsable",
            "snapshot.NormalizedProgress >= RewardOptimizationMinProgress",
            "HasStableLead(snapshot)",
            "enchantedPoundUsable",
        ):
            self.assertIn(required, core)

    def test_decision_priority_contract_is_explicit(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/AiPowerUpUsagePolicy.cs")
        core_start = source.index("private static AiPowerUpDecision DecideCore(")
        core_end = source.index("private static InventoryIndex BuildInventoryIndex", core_start)
        core = source[core_start:core_end]

        shield = core.index("PowerUpKind.EyeShield")
        asphalt = core.index("PowerUpKind.AsphaltShard", shield)
        nitro = core.index("PowerUpKind.NitroSpirit", asphalt)
        traffic = core.index("PowerUpKind.TrafficCurse", nitro)
        reward = core.index("PowerUpKind.EnchantedPound", traffic)
        self.assertLess(shield, asphalt)
        self.assertLess(asphalt, nitro)
        self.assertLess(nitro, traffic)
        self.assertLess(traffic, reward)

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
