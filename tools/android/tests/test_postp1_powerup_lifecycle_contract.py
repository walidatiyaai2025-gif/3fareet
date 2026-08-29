import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class PostP1PowerUpLifecycleContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_known_powerup_kinds_and_policies_are_explicit(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpEffectState.cs")

        for required in (
            "AsphaltShard = 0",
            "NitroSpirit = 1",
            "TrafficCurse = 2",
            "EnchantedPound = 3",
            "EyeShield = 4",
            "RefreshDuration = 0",
            "IgnoreWhileActive = 1",
            "ReplaceIfStronger = 2",
        ):
            self.assertIn(required, source)

        self.assertNotIn("using UnityEngine;", source)

    def test_state_is_race_scoped_deterministic_and_fail_closed(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpEffectState.cs")

        for required in (
            "Dictionary<PowerUpKind, ActivePowerUpEffect>",
            "RemoveExpired(raceTimeSeconds)",
            "activeEffects[spec.Kind] = new ActivePowerUpEffect(spec, raceTimeSeconds)",
            "spec.Magnitude > existing.Spec.Magnitude",
            "snapshot.Sort((left, right) => left.Spec.Kind.CompareTo(right.Spec.Kind))",
            "public void ResetRace()",
            "activeEffects.Clear();",
            "double.IsNaN",
            "double.IsInfinity",
        ):
            self.assertIn(required, source)

    def test_eye_shield_blocks_only_retained_hostile_effects(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpEffectState.cs")

        self.assertIn(
            "PowerUpEffectPolicy.IsHostile(spec.Kind) && IsActive(PowerUpKind.EyeShield, raceTimeSeconds)",
            source,
        )
        self.assertIn("case PowerUpKind.AsphaltShard:", source)
        self.assertIn("case PowerUpKind.TrafficCurse:", source)
        self.assertIn("return PowerUpApplyResult.BlockedByEyeShield;", source)

    def test_editmode_tests_cover_refresh_stack_immunity_expiry_and_reset(self):
        tests = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/PowerUpEffectStateTests.cs")
        asmdef = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/Afareet.RaceEditModeTests.asmdef")

        for required in (
            "Apply_CreatesSingleActiveEffectPerKind",
            "RefreshDuration_RenewsFromApplicationTimeWithoutDuplication",
            "IgnoreWhileActive_DoesNotChangeExistingEffect",
            "ReplaceIfStronger_ReplacesOnlyStrictlyStrongerMagnitude",
            "EyeShield_BlocksHostileEffectsButAllowsBeneficialEffects",
            "ExpirationAndTick_AreDeterministicAtBoundary",
            "Snapshot_IsSortedByKindAndResetRaceClearsEverything",
            "Spec_RejectsInvalidDurationMagnitudeAndEnumValues",
        ):
            self.assertIn(required, tests)

        self.assertIn('"Afareet.Race"', asmdef)

    def test_unity_metadata_is_tracked(self):
        source_meta = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpEffectState.cs.meta")
        tests_meta = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/PowerUpEffectStateTests.cs.meta")

        for content in (source_meta, tests_meta):
            self.assertIn("fileFormatVersion: 2", content)
            self.assertIn("guid:", content)


if __name__ == "__main__":
    unittest.main()
