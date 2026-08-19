import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class PostP1PowerUpPresentationContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_presentation_contract_is_typed_decoupled_and_unity_free(self):
        hooks = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpPresentationHooks.cs")

        for required in (
            "PowerUpPresentationEventKind",
            "Applied = 0",
            "Refreshed = 1",
            "Replaced = 2",
            "Blocked = 3",
            "Expired = 4",
            "RaceReset = 5",
            "IPowerUpPresentationSink",
            "NullPowerUpPresentationSink",
            "SequenceId",
            "PowerUpKind? Kind",
        ):
            self.assertIn(required, hooks)

        for forbidden in ("using UnityEngine;", "AudioSource", "ParticleSystem", "VisualEffect"):
            self.assertNotIn(forbidden, hooks)

    def test_state_emits_only_real_transitions_and_reset(self):
        state = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpEffectState.cs")

        for required in (
            "PowerUpEffectState(IPowerUpPresentationSink presentationSink)",
            "presentationSink ?? NullPowerUpPresentationSink.Instance",
            "PowerUpPresentationEventKind.Applied",
            "PowerUpPresentationEventKind.Refreshed",
            "PowerUpPresentationEventKind.Replaced",
            "PowerUpPresentationEventKind.Blocked",
            "PowerUpPresentationEventKind.Expired",
            "PowerUpPresentationEventKind.RaceReset",
            "private static readonly PowerUpKind[] AllPowerUpKinds",
            "nextPresentationSequenceId++;",
        ):
            self.assertIn(required, state)

        expiry_start = state.index("private int RemoveExpired(double raceTimeSeconds)")
        expiry_end = state.index("private void EmitPresentation", expiry_start)
        expiry = state[expiry_start:expiry_end]
        self.assertIn("for (var index = 0; index < AllPowerUpKinds.Length; index++)", expiry)
        self.assertIn("activeEffects.TryGetValue(kind, out var effect)", expiry)
        self.assertIn("PowerUpPresentationEventKind.Expired", expiry)
        self.assertNotIn("new List<PowerUpKind>", expiry)
        self.assertNotIn(".Sort(", expiry)

        ignore_case = "case PowerUpRefreshPolicy.IgnoreWhileActive:\n                    return PowerUpApplyResult.IgnoredWhileActive;"
        weak_replace = "return PowerUpApplyResult.IgnoredWhileActive;"
        self.assertIn(ignore_case, state)
        self.assertGreaterEqual(state.count(weak_replace), 2)
        self.assertNotIn("using UnityEngine;", state)

    def test_editmode_tests_lock_sequence_ordering_and_no_false_cues(self):
        tests = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/PowerUpPresentationHookTests.cs")

        for required in (
            "StateWithoutSink_RemainsValidAndDoesNotRequirePresentationRuntime",
            "ApplyRefreshReplaceAndBlock_EmitTypedStrictlyIncreasingCues",
            "IgnoredApplications_DoNotEmitFalsePositivePresentationCues",
            "Tick_EmitsExpiredCuesInPowerUpKindOrder",
            "ResetRace_ClearsStateAndEmitsExactlyOneResetCue",
            "new long[] { 1, 2, 3, 4, 5 }",
            "new long[] { 4, 5, 6 }",
            "RecordingSink : IPowerUpPresentationSink",
        ):
            self.assertIn(required, tests)

    def test_compile_contract_includes_domain_and_hook_sources(self):
        project = self._read("tools/android/contracts/PowerUpPresentationCompile.csproj")
        self.assertIn("<TargetFramework>netstandard2.1</TargetFramework>", project)
        self.assertIn("PowerUpEffectState.cs", project)
        self.assertIn("PowerUpPresentationHooks.cs", project)
        self.assertIn("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", project)

    def test_unity_metadata_is_present_for_new_files(self):
        source_meta = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpPresentationHooks.cs.meta")
        test_meta = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/PowerUpPresentationHookTests.cs.meta")
        self.assertIn("fileFormatVersion: 2", source_meta)
        self.assertIn("guid:", source_meta)
        self.assertIn("fileFormatVersion: 2", test_meta)
        self.assertIn("guid:", test_meta)


if __name__ == "__main__":
    unittest.main()
