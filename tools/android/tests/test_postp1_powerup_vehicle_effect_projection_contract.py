import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class PostP1PowerUpVehicleEffectProjectionContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_projection_contract_is_typed_bounded_and_unity_free(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpVehicleEffectProjection.cs")

        for required in (
            "PowerUpVehicleEffectProjection",
            "AccelerationMultiplier",
            "MaxSpeedMultiplier",
            "SteeringAuthorityMultiplier",
            "GripMultiplier",
            "RewardMultiplier",
            "SourceEffectCount",
            "MinimumDriveMultiplier = .25d",
            "MaximumBoostMultiplier = 2d",
            "MinimumHandlingMultiplier = .25d",
            "MaximumRewardMultiplier = 5d",
            "HashSet<PowerUpKind>",
            "Duplicate active power-up effect",
        ):
            self.assertIn(required, source)

        self.assertNotIn("using UnityEngine;", source)
        self.assertNotIn("MonoBehaviour", source)

    def test_retained_effect_mapping_is_explicit_and_eye_shield_is_neutral(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpVehicleEffectProjection.cs")

        for required in (
            "case PowerUpKind.AsphaltShard:",
            "steeringAuthority *= handlingPenalty;",
            "grip *= handlingPenalty;",
            "case PowerUpKind.NitroSpirit:",
            "acceleration *= boost;",
            "maxSpeed *= boost;",
            "case PowerUpKind.TrafficCurse:",
            "acceleration *= slow;",
            "maxSpeed *= slow;",
            "case PowerUpKind.EnchantedPound:",
            "reward *= Clamp(",
            "case PowerUpKind.EyeShield:",
            "does not alter drive or reward multipliers",
        ):
            self.assertIn(required, source)

    def test_runtime_extension_reads_authoritative_active_effects(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntimeProjectionExtensions.cs")

        for required in (
            "GetVehicleEffectProjection(",
            "this PowerUpRaceRuntime runtime",
            "foreach (PowerUpKind kind in Enum.GetValues(typeof(PowerUpKind)))",
            "runtime.GetActiveEffect(racerId, kind, raceTimeSeconds)",
            "PowerUpVehicleEffectProjectionPolicy.Project(activeEffects)",
        ):
            self.assertIn(required, source)

        self.assertNotIn("using UnityEngine;", source)

    def test_behavior_and_editmode_regressions_cover_projection_semantics(self):
        runner = self._read("tools/android/contracts/PowerUpVehicleEffectProjectionContractRunner.cs")
        tests = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/PowerUpVehicleEffectProjectionTests.cs")

        for required in (
            "NeutralAndEyeShieldContract();",
            "IndividualProjectionContract();",
            "CompositionOrderContract();",
            "DuplicateKindContract();",
            "RuntimeAuthorityAndExpiryContract();",
            "Power-up vehicle effect projection behavior contract: PASS",
        ):
            self.assertIn(required, runner)

        for required in (
            "EmptyAndEyeShieldOnly_ProjectNeutralModifiers",
            "RetainedEffects_ProjectIntendedBoundedModifierFamilies",
            "NitroAndTraffic_ComposeOrderIndependently",
            "DuplicateKinds_FailClosed",
            "RuntimeProjection_UsesAuthoritativeStateAndExpiresWithIt",
        ):
            self.assertIn(required, tests)

    def test_compile_projects_include_projection_sources_without_project_reference_collision(self):
        runtime_project = self._read("tools/android/contracts/PowerUpRuntimeCompile.csproj")
        runner_project = self._read("tools/android/contracts/PowerUpVehicleEffectProjectionContractRunner.csproj")

        for source_name in (
            "PowerUpVehicleEffectProjection.cs",
            "PowerUpRaceRuntimeProjectionExtensions.cs",
        ):
            self.assertIn(source_name, runtime_project)
            self.assertIn(source_name, runner_project)

        self.assertIn("<TargetFramework>net8.0</TargetFramework>", runner_project)
        self.assertIn(
            "<BaseIntermediateOutputPath>obj/PowerUpVehicleEffectProjectionContractRunner/</BaseIntermediateOutputPath>",
            runner_project,
        )
        self.assertIn(
            "<BaseOutputPath>bin/PowerUpVehicleEffectProjectionContractRunner/</BaseOutputPath>",
            runner_project,
        )
        self.assertNotIn("<ProjectReference", runner_project)

    def test_new_unity_files_have_metadata(self):
        for relative in (
            "unity_game/Assets/Afareet/Scripts/Race/PowerUpVehicleEffectProjection.cs.meta",
            "unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntimeProjectionExtensions.cs.meta",
            "unity_game/Assets/Afareet/Tests/EditMode/Race/PowerUpVehicleEffectProjectionTests.cs.meta",
        ):
            content = self._read(relative)
            self.assertIn("fileFormatVersion: 2", content)
            self.assertIn("guid:", content)


if __name__ == "__main__":
    unittest.main()
