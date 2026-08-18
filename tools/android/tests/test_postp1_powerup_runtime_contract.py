import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class PostP1PowerUpRuntimeContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_runtime_is_pure_csharp_and_owns_authoritative_inventory(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntime.cs")
        for required in (
            "public sealed class PowerUpRaceRuntime", "SortedDictionary<string, RacerState>", "PowerUpRuntimeRuleset",
            "GetInventorySnapshot", "GetAiAvailability", "IsPowerUpUsable", "TryUse(", "ExecuteAiDecision(", "TickAll(", "ResetRace()",
            "slot.Charges--", "slot.ReadyAtSeconds = raceTimeSeconds + rule.CooldownSeconds",
        ):
            self.assertIn(required, source)
        self.assertNotIn("using UnityEngine;", source)
        self.assertNotIn("MonoBehaviour", source)

    def test_single_slot_usability_query_is_allocation_free(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntime.cs")
        start = source.index("public bool IsPowerUpUsable(")
        end = source.index("public PowerUpRuntimeUseResult TryUse(", start)
        method = source[start:end]
        helper_start = source.index("private static bool IsSlotUsable(")
        helper_end = source.index("private static PowerUpRuntimeUseResult GateResult(", helper_start)
        helper = source[helper_start:helper_end]

        self.assertIn("var racer = GetRacerOrThrow(racerId);", method)
        self.assertIn("return IsSlotUsable(racer.Inventory[kind], raceTimeSeconds);", method)
        self.assertIn("return slot.Charges > 0 && slot.ReadyAtSeconds <= raceTimeSeconds;", helper)
        for forbidden in (
            "GetInventorySnapshot(",
            "GetAiAvailability(",
            "new List<",
            "new PowerUpInventorySnapshot",
            "new AiPowerUpAvailability",
            ".AsReadOnly()",
        ):
            self.assertNotIn(forbidden, method)
            self.assertNotIn(forbidden, helper)

    def test_live_ai_execution_reads_slots_without_availability_snapshot_allocations(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntime.cs")
        start = source.index("public AiPowerUpExecutionResult ExecuteAiDecision(")
        end = source.index("public IReadOnlyList<PowerUpRuntimeTickResult> TickAll", start)
        method = source[start:end]

        self.assertIn("ValidateRaceTime(raceTimeSeconds);", method)
        self.assertIn("var source = GetRacerOrThrow(sourceRacerId);", method)
        self.assertIn("var decision = AiPowerUpUsagePolicy.Decide(", method)
        for kind in (
            "PowerUpKind.AsphaltShard",
            "PowerUpKind.NitroSpirit",
            "PowerUpKind.TrafficCurse",
            "PowerUpKind.EnchantedPound",
            "PowerUpKind.EyeShield",
        ):
            self.assertIn(f"IsSlotUsable(source.Inventory[{kind}], raceTimeSeconds)", method)

        self.assertNotIn("GetAiAvailability(", method)
        self.assertNotIn("GetInventorySnapshot(", method)
        self.assertNotIn("new List<", method)
        self.assertNotIn("new AiPowerUpAvailability", method)

    def test_runtime_commits_only_real_effect_transitions(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntime.cs")
        ignored_index = source.rindex("if (applyResult == PowerUpApplyResult.IgnoredWhileActive)")
        consume_index = source.rindex("slot.Charges--")
        self.assertLess(ignored_index, consume_index)
        tail = source[ignored_index:]
        self.assertIn("PowerUpRuntimeUseStatus.IgnoredByEffectPolicy", tail)
        self.assertIn("PowerUpRuntimeUseStatus.BlockedByEyeShield", tail)
        self.assertIn("applyResult == PowerUpApplyResult.BlockedByEyeShield", tail)
        self.assertIn("if (consumeInventory)", tail)
        self.assertIn("rule.TargetMode == PowerUpRuntimeTargetMode.WorldDeployable", source)
        self.assertIn("consumeInventory: false", source)

    def test_effect_tick_does_not_allocate_temporary_expiry_collections(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpEffectState.cs")
        start = source.index("private int RemoveExpired(double raceTimeSeconds)")
        end = source.index("private void EmitPresentation(", start)
        remove_expired = source[start:end]

        self.assertIn(
            "private static readonly PowerUpKind[] AllPowerUpKinds =",
            source,
        )
        self.assertIn("for (var index = 0; index < AllPowerUpKinds.Length; index++)", remove_expired)
        self.assertIn("activeEffects.TryGetValue(kind, out var effect)", remove_expired)
        self.assertIn("activeEffects.Remove(kind);", remove_expired)
        self.assertNotIn("new List<PowerUpKind>", remove_expired)
        self.assertNotIn("expiredKinds.Sort", remove_expired)
        self.assertNotIn("foreach (var pair in activeEffects)", remove_expired)

    def test_target_modes_lock_retained_gameplay_semantics(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntime.cs")
        for required in (
            "case PowerUpKind.AsphaltShard:", "return PowerUpRuntimeTargetMode.WorldDeployable;",
            "case PowerUpKind.TrafficCurse:", "return PowerUpRuntimeTargetMode.Opponent;",
            "case PowerUpKind.NitroSpirit:", "case PowerUpKind.EnchantedPound:", "case PowerUpKind.EyeShield:",
            "return PowerUpRuntimeTargetMode.Self;", "PowerUpRuntimeUseStatus.MissingTarget",
            "PowerUpRuntimeUseStatus.InvalidTarget", "PowerUpRuntimeUseStatus.UnknownTarget",
        ):
            self.assertIn(required, source)

    def test_ai_execution_uses_authoritative_slots_and_try_use(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntime.cs")
        self.assertIn("var source = GetRacerOrThrow(sourceRacerId);", source)
        self.assertIn("var decision = AiPowerUpUsagePolicy.Decide(", source)
        self.assertIn("var useResult = TryUse(sourceRacerId, kind, targetRacerId, raceTimeSeconds);", source)
        self.assertIn("kind == PowerUpKind.TrafficCurse", source)
        self.assertIn("rule.TargetMode == PowerUpRuntimeTargetMode.WorldDeployable", source)
        self.assertIn("TryApplyDeployedEffect", source)
        self.assertIn("kind != PowerUpKind.AsphaltShard", source)

    def test_behavior_runner_and_unity_regressions_cover_required_paths(self):
        runner = self._read("tools/android/contracts/PowerUpRuntimeContractRunner.cs")
        tests = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/PowerUpRaceRuntimeTests.cs")
        for required in (
            "InventoryAndCooldownContract", "EyeShieldConsumptionContract", "IgnoredUseContract", "AiExecutionContract",
            "TargetGateContract", "DeployableTrapFlow", "TickAndResetContract", "Power-up runtime behavior contract: PASS",
        ):
            self.assertIn(required, runner)
        for required in (
            "TryUse_ConsumesChargeAndEnforcesCooldownFromOneAuthoritativeSlot",
            "IsPowerUpUsable_TracksChargesAndCooldownWithoutInventorySnapshot",
            "HostileUseBlockedByEyeShield_IsStillConsumedAsRealAttempt",
            "IgnoredEffectPolicy_DoesNotConsumeChargeOrStartCooldown",
            "AiAvailabilityAndExecution_UseTheSameInventoryAndTryUsePath",
            "TargetedAiDecision_UsesCallerSuppliedDeterministicOpponent",
            "TickAll_IsStableByRacerId_AndResetRestoresRaceScopedState",
            "InvalidTargetAndMissingTarget_FailClosedWithoutConsumption",
        ):
            self.assertIn(required, tests)

    def test_compile_projects_cover_all_runtime_dependencies(self):
        project = self._read("tools/android/contracts/PowerUpRuntimeCompile.csproj")
        runner_project = self._read("tools/android/contracts/PowerUpRuntimeContractRunner.csproj")
        self.assertIn("<TargetFramework>netstandard2.1</TargetFramework>", project)
        self.assertIn("<TargetFramework>net8.0</TargetFramework>", runner_project)
        self.assertIn("<BaseIntermediateOutputPath>obj/PowerUpRuntimeContractRunner/</BaseIntermediateOutputPath>", runner_project)
        self.assertIn("<BaseOutputPath>bin/PowerUpRuntimeContractRunner/</BaseOutputPath>", runner_project)
        for source in (
            "PowerUpPresentationHooks.cs", "PowerUpEffectState.cs", "AiPowerUpUsagePolicy.cs", "PowerUpRaceRuntime.cs",
            "AsphaltShardTrapRuntime.cs",
        ):
            self.assertIn(source, project)
            self.assertIn(source, runner_project)
        self.assertIn("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", project)
        self.assertIn("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", runner_project)
        self.assertNotIn("ProjectReference", runner_project)

    def test_unity_metadata_is_present_for_runtime_and_tests(self):
        source_meta = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntime.cs.meta")
        test_meta = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/PowerUpRaceRuntimeTests.cs.meta")
        self.assertIn("fileFormatVersion: 2", source_meta)
        self.assertIn("guid:", source_meta)
        self.assertIn("fileFormatVersion: 2", test_meta)
        self.assertIn("guid:", test_meta)


if __name__ == "__main__":
    unittest.main()
