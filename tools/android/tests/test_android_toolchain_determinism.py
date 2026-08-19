import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
BUILD_CS = REPO_ROOT / "unity_game/Assets/Afareet/Editor/AfareetBuild.cs"
BUILD_PS = REPO_ROOT / "tools/android/build_experimental_windows.ps1"
GITIGNORE = REPO_ROOT / ".gitignore"


class AndroidToolchainDeterminismTests(unittest.TestCase):
    def test_android_target_is_explicit_api_36(self):
        source = BUILD_CS.read_text(encoding="utf-8-sig")
        self.assertIn(
            "PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;",
            source,
        )

    def test_build_code_does_not_implicitly_prefer_localappdata_android_sdk(self):
        source = BUILD_CS.read_text(encoding="utf-8-sig")
        self.assertNotIn("Environment.SpecialFolder.LocalApplicationData", source)
        self.assertIn('"Data", "PlaybackEngines", "AndroidPlayer"', source)
        self.assertIn('Path.Combine(androidPlayer, "SDK")', source)
        self.assertIn('Path.Combine(androidPlayer, "NDK")', source)
        self.assertIn('Path.Combine(androidPlayer, "OpenJDK")', source)
        self.assertIn('Path.Combine(androidSdk, "platforms", "android-36")', source)

    def test_experimental_runner_pins_unity_hub_sdk_for_child_process(self):
        source = BUILD_PS.read_text(encoding="utf-8-sig")
        self.assertIn('$UnitySdk = Join-Path $androidPlayer "SDK"', source)
        self.assertIn('$env:AFAREET_ANDROID_SDK_ROOT = $UnitySdk', source)
        self.assertIn('platforms\\android-36', source)
        self.assertIn("AFAREET_EXPERIMENTAL_ANDROID_SDK_PIN", source)

    def test_performance_test_build_outputs_are_ignored(self):
        source = GITIGNORE.read_text(encoding="utf-8-sig")
        self.assertIn("unity_game/Assets/Resources/PerformanceTestRunInfo.json", source)
        self.assertIn("unity_game/Assets/Resources/PerformanceTestRunSettings.json", source)
        self.assertIn("unity_game/Assets/Resources.meta", source)
        self.assertIn("unity_game/Assets/Afareet/Resources/Art/Architecture.meta", source)
        self.assertIn(
            "unity_game/Assets/Afareet/Resources/Art/Architecture/CairoLandmarks.meta",
            source,
        )
        self.assertIn("unity_game/Assets/Afareet/Resources/Art/TracksEnvironments.meta", source)


if __name__ == "__main__":
    unittest.main()
