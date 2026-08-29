import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class PostP1ApplyDriveModifiersContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_vehicle_modifier_is_pure_and_vehicle_assembly_stays_race_free(self):
        modifier = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/ArcadeDriveModifier.cs")
        asmdef = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/Afareet.Vehicle.asmdef")

        for required in (
            "public readonly struct ArcadeDriveModifier",
            "MinimumMultiplier = .25d",
            "MaximumMultiplier = 2d",
            "public bool IsValid => initialized;",
            "ValidateInitialized",
            "public static ArcadeDriveModifier Neutral()",
        ):
            self.assertIn(required, modifier)

        self.assertNotIn("using UnityEngine;", modifier)
        self.assertNotIn("Afareet.Race", modifier)
        self.assertNotIn("Afareet.Race", asmdef)

    def test_arcade_controller_applies_only_explicit_drive_axes(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/ArcadeCarController.cs")

        for required in (
            "private ArcadeDriveModifier externalDriveModifier = ArcadeDriveModifier.Neutral();",
            "public ArcadeDriveModifier ExternalDriveModifier => externalDriveModifier;",
            "var driveModifier = externalDriveModifier;",
            "(float)driveModifier.AccelerationMultiplier",
            "(float)driveModifier.MaxSpeedMultiplier",
            "(float)driveModifier.SteeringAuthorityMultiplier",
            "(float)driveModifier.GripMultiplier",
            "ArcadeDriveModifier.ValidateInitialized(modifier, nameof(modifier));",
            "ResetExternalDriveModifier();",
        ):
            self.assertIn(required, source)

        self.assertGreaterEqual(source.count("(float)driveModifier.MaxSpeedMultiplier"), 2)
        self.assertIn(
            "body.AddForce(transform.forward * (config.nitroForce * forceScale), ForceMode.Acceleration);",
            source,
        )
        self.assertIn("VehicleSpiritPolicy.StepNitroEnergy(", source)
        self.assertNotIn("RewardMultiplier", source)
        self.assertNotIn("Afareet.Race", source)

    def test_race_director_is_single_projection_translation_point(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/RaceDirector.cs")

        for required in (
            "[DefaultExecutionOrder(-200)]",
            "private void ApplyPowerUpDriveModifiers(double raceTimeSeconds)",
            "powerUpRuntime.GetVehicleEffectProjection(runtime.RacerId, raceTimeSeconds)",
            "runtime.Car.SetExternalDriveModifier(new ArcadeDriveModifier(",
            "projection.AccelerationMultiplier",
            "projection.MaxSpeedMultiplier",
            "projection.SteeringAuthorityMultiplier",
            "projection.GripMultiplier",
            "private void ResetPowerUpDriveModifiers()",
        ):
            self.assertIn(required, source)

        self.assertNotIn("projection.RewardMultiplier", source)

    def test_projection_recomputes_only_on_effect_change_and_resets_on_boundaries(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/RaceDirector.cs")

        for required in (
            "var tickResults = powerUpRuntime.TickAll(raceTimeSeconds);",
            "tickResults[i].ExpiredEffectCount > 0",
            "execution.UseResult.Status == PowerUpRuntimeUseStatus.Used",
            "if (driveProjectionDirty)",
            "ApplyPowerUpDriveModifiers(raceTimeSeconds);",
            "car.ResetExternalDriveModifier();",
            "ResetPowerUpDriveModifiers();",
            "powerUpRuntime.ResetRace();",
        ):
            self.assertIn(required, source)

        tick_index = source.index("var tickResults = powerUpRuntime.TickAll(raceTimeSeconds);")
        decision_index = source.index("var execution = ai.EvaluateBoundPowerUpDecision();")
        apply_index = source.index("ApplyPowerUpDriveModifiers(raceTimeSeconds);")
        self.assertLess(tick_index, decision_index)
        self.assertLess(decision_index, apply_index)

    def test_modifier_compile_and_behavior_contracts_are_independent(self):
        compile_project = self._read("tools/android/contracts/ArcadeDriveModifierCompile.csproj")
        runner_project = self._read("tools/android/contracts/ArcadeDriveModifierContractRunner.csproj")
        runner = self._read("tools/android/contracts/ArcadeDriveModifierContractRunner.cs")

        self.assertIn("<TargetFramework>netstandard2.1</TargetFramework>", compile_project)
        self.assertIn("ArcadeDriveModifier.cs", compile_project)
        self.assertIn("<TargetFramework>net8.0</TargetFramework>", runner_project)
        self.assertIn(
            "<BaseIntermediateOutputPath>obj/ArcadeDriveModifierContractRunner/</BaseIntermediateOutputPath>",
            runner_project,
        )
        self.assertIn("ArcadeDriveModifier.cs", runner_project)

        for required in (
            "NeutralContract();",
            "BoundsContract();",
            "DefaultFailsClosedContract();",
            "RepresentativeProjectionContract();",
            "Arcade drive modifier behavior contract: PASS",
        ):
            self.assertIn(required, runner)

    def test_new_unity_sources_have_metadata(self):
        for relative in (
            "unity_game/Assets/Afareet/Scripts/Vehicle/ArcadeDriveModifier.cs.meta",
            "unity_game/Assets/Afareet/Tests/EditMode/Vehicle/ArcadeDriveModifierTests.cs.meta",
        ):
            content = self._read(relative)
            self.assertIn("fileFormatVersion: 2", content)
            self.assertIn("guid:", content)


if __name__ == "__main__":
    unittest.main()
