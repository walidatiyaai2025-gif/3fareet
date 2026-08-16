import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class PostP1PowerUpRuntimeContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_runtime_is_pure_csharp_and_owns_authoritative_inventory(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntime.cs")

        for required in (
            "public sealed class PowerUpRaceRuntime",
            "SortedDictionary<string, RacerState>",
            "PowerUpRuntimeRuleset",
            "GetInventorySnapshot",
            "GetAiAvailability",
            "TryUse(",
            "ExecuteAiDecision(",
            "TickAll(",
            "ResetRace()",
            "slot.Charges--",
            "slot.ReadyAtSeconds = raceTimeSeconds + rule.CooldownSeconds",
        ):
            self.assertIn(required, source)

        self.assertNotIn("using UnityEngine;", source)
        self.assertNotIn("MonoBehaviour", source)

    def test_runtime_commits_only_real_effect_transitions(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntime.cs")

        ignored_index = source.index("if (applyResult == PowerUpApplyResult.IgnoredWhileActive)")
        consume_index = source.index("slot.Charges--")
        self.assertLess(ignored_index, consume_index)
        self.assertIn("PowerUpRuntimeUseStatus.IgnoredByEffectPolicy", source)
        self.assertIn("PowerUpRuntimeUseStatus.BlockedByEyeShield", source)
        self.assertIn("applyResult == PowerUpApplyResult.BlockedByEyeShield", source)

    def test_target_modes_lock_retained_gameplay_semantics(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntime.cs")

        for required in (
            "case PowerUpKind.AsphaltShard:",
            "case PowerUpKind.TrafficCurse:",
            "return PowerUpRuntimeTargetMode.Opponent;",
            "case PowerUpKind.NitroSpirit:",
            "case PowerUpKind.EnchantedPound:",
            "case PowerUpKind.EyeShield:",
            "return PowerUpRuntimeTargetMode.Self;",
            "PowerUpRuntimeUseStatus.MissingTarget",
            "PowerUpRuntimeUseStatus.InvalidTarget",
            "PowerUpRuntimeUseStatus.UnknownTarget",
        ):
            self.assertIn(required, source)

    def test_ai_execution_uses_authoritative_availability_and_try_use(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntime.cs")

        self.assertIn("var availability = GetAiAvailability(sourceRacerId, raceTimeSeconds);", source)
        self.assertIn("var decision = AiPowerUpUsagePolicy.Decide(snapshot, availability);", source)
        self.assertIn("var useResult = TryUse(sourceRacerId, kind, targetRacerId, raceTimeSeconds);", source)
        self.assertIn("kind == PowerUpKind.TrafficCurse", source)
        self.assertIn("kind == PowerUpKind.AsphaltShard", source)

    def test_behavior_runner_and_unity_regressions_cover_required_paths(self):
        runner = self._read("tools/android/contracts/PowerUpRuntimeContractRunner.cs")
        tests = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/PowerUpRaceRuntimeTests.cs")

        for required in (
            "InventoryAndCooldownContract",
            "EyeShieldConsumptionContract",
            "IgnoredUseContract",
            "AiExecutionContract",
            "TargetGateContract",
            "TickAndResetContract",
            "Power-up runtime behavior contract: PASS",
        ):
            self.assertIn(required, runner)

        for required in (
            "TryUse_ConsumesChargeAndEnforcesCooldownFromOneAuthoritativeSlot",
            "HostileUseBlockedByEyeShield_IsStillConsumedAsRealAttempt",
            "IgnoredEffectPolicy_DoesNotConsumeChargeOrStartCooldown",
            "AiAvailabilityAndExecution_UseTheSameInventoryAndTryUsePath",
            "TargetedAiDecision_UsesCallerSuppliedDeterministicOpponent",
            "TickAll_IsStableByRacerId_AndResetRestoresRaceScopedState",
            "InvalidTargetAndMissingTarget_FailClosedWithoutConsumption",
        ):
            self.assertIn(required, tests)

    def test_compile_project_covers_all_runtime_dependencies(self):
        project = self._read("tools/android/contracts/PowerUpRuntimeCompile.csproj")
        runner_project = self._read("tools/android/contracts/PowerUpRuntimeContractRunner.csproj")

        self.assertIn("<TargetFramework>netstandard2.1</TargetFramework>", project)
        for source in (
            "PowerUpPresentationHooks.cs",
            "PowerUpEffectState.cs",
            "AiPowerUpUsagePolicy.cs",
            "PowerUpRaceRuntime.cs",
        ):
            self.assertIn(source, project)
        self.assertIn("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", project)
        self.assertIn("<TargetFramework>net8.0</TargetFramework>", runner_project)
        self.assertIn("<ProjectReference Include=\"PowerUpRuntimeCompile.csproj\" />", runner_project)

    def test_unity_metadata_is_present_for_runtime_and_tests(self):
        source_meta = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntime.cs.meta")
        test_meta = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/PowerUpRaceRuntimeTests.cs.meta")
        self.assertIn("fileFormatVersion: 2", source_meta)
        self.assertIn("guid:", source_meta)
        self.assertIn("fileFormatVersion: 2", test_meta)
        self.assertIn("guid:", test_meta)


if __name__ == "__main__":
    unittest.main()
