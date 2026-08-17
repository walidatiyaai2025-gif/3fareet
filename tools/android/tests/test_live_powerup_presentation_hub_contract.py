import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
RACE_DIRECTOR = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/RaceDirector.cs"
HUB = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/PowerUpPresentationHub.cs"
HOOKS = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/PowerUpPresentationHooks.cs"


class LivePowerUpPresentationHubContractTests(unittest.TestCase):
    def test_live_racer_registrations_use_racer_scoped_presentation_sink(self):
        source = RACE_DIRECTOR.read_text(encoding="utf-8")
        self.assertIn("PowerUpPresentationHub.CreateSink(racerId)", source)
        self.assertIn("new PowerUpRacerRegistration(\n                    racerId,", source)
        self.assertNotIn(
            "registrations.Add(new PowerUpRacerRegistration(racers[i].RacerId));",
            source,
        )

    def test_hub_preserves_racer_identity_and_typed_domain_event(self):
        source = HUB.read_text(encoding="utf-8")
        for required in (
            "RacerPowerUpPresentationEvent",
            "public string RacerId",
            "public PowerUpPresentationEvent Event",
            "IPowerUpPresentationSink",
            "CreateSink",
            "Published?.Invoke",
        ):
            self.assertIn(required, source)

    def test_hub_has_no_unity_audio_or_visual_dependency(self):
        source = HUB.read_text(encoding="utf-8")
        for forbidden in (
            "using UnityEngine",
            "AudioClip",
            "AudioSource",
            "ParticleSystem",
            "GameObject",
            "Resources.Load",
            "new Mesh",
        ):
            self.assertNotIn(forbidden, source)

    def test_domain_hooks_keep_null_sink_for_non_live_unit_contexts(self):
        source = HOOKS.read_text(encoding="utf-8")
        self.assertIn("NullPowerUpPresentationSink", source)
        self.assertIn("IPowerUpPresentationSink", source)


if __name__ == "__main__":
    unittest.main()
