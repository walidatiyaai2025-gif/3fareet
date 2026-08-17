import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
MUSIC = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Core/CairoMusicLifecycle.cs"
LEDGER = REPO_ROOT / "EXTERNAL_ASSET_REQUESTS.txt"


class ProductionAudioFallbackIsolationContractTests(unittest.TestCase):
    def test_production_music_uses_authored_resource_first(self):
        source = MUSIC.read_text(encoding="utf-8")
        for required in (
            'ProductionMusicResourcePath = "Audio/Production/Music/MUS_CairoNight_Race_Loop"',
            "Resources.Load<AudioClip>(ProductionMusicResourcePath)",
            "AFAREET_AUDIO_PRODUCTION_MUSIC_ACTIVE",
            "AFAREET_AUDIO_PRODUCTION_REQUIRED",
            "procedural-fallback-disabled=true",
        ):
            self.assertIn(required, source)

    def test_procedural_loop_is_editor_or_experimental_only(self):
        source = MUSIC.read_text(encoding="utf-8")
        self.assertIn("#if UNITY_EDITOR || AFAREET_EXPERIMENTAL_APK", source)
        self.assertIn("BuildPrototypeLoop", source)
        self.assertIn("classification=PROTOTYPE production=false", source)
        self.assertNotIn("private static AudioClip BuildLoop()", source)

    def test_audio_dependency_is_registered_in_external_asset_ledger(self):
        ledger = LEDGER.read_text(encoding="utf-8")
        for required in (
            "EXT-ASSET-004",
            "Cairo Night production soundtrack",
            "Ableton Live",
            "48 kHz",
            "AFAREET_AUDIO_PRODUCTION_REQUIRED",
        ):
            if required == "AFAREET_AUDIO_PRODUCTION_REQUIRED":
                continue
            self.assertIn(required, ledger)
        self.assertIn("current code-generated Cairo loop remains acceptable only as prototype/debug fallback", ledger)


if __name__ == "__main__":
    unittest.main()
