import unittest
from pathlib import Path


REPO = Path(__file__).resolve().parents[3]
EDITOR = REPO / "unity_game" / "Assets" / "Afareet" / "Editor"
VEHICLE = REPO / "unity_game" / "Assets" / "Afareet" / "Scripts" / "Vehicle"


class ExperimentalApkGateIsolationContractTests(unittest.TestCase):
    def read(self, path: Path) -> str:
        return path.read_text(encoding="utf-8")

    def test_dedicated_experimental_identity_is_strict(self):
        text = self.read(EDITOR / "AfareetBuildContext.cs")
        for required in (
            'ExperimentalAndroidOutput',
            'Builds/Android/afareet-unity3d-experimental.apk',
            'IsDedicatedExperimentalAndroidBuild(BuildReport report)',
            'IsExperimentalAndroidBuild',
            'report.summary.platform != BuildTarget.Android',
            '(report.summary.options & BuildOptions.Development) == 0',
            'StringComparison.OrdinalIgnoreCase',
        ):
            self.assertIn(required, text)

    def test_all_visual_production_acceptance_gates_skip_only_dedicated_experimental_build(self):
        gate_files = (
            "HeroCarProductionBuildPreprocessor.cs",
            "RivalProductionBuildGate.cs",
            "P1ProductionLandmarkBuildGate.cs",
            "P1ProductionLandmarkMaterialDependencyGate.cs",
            "P1ProductionTrackDressingBuildGate.cs",
            "P1ProductionTrackDressingMaterialDependencyGate.cs",
            "P1ProductionMobileLodBuildGate.cs",
            "P1ProductionRoadCurbMobileLodBuildGate.cs",
            "P1ProductionRoadsideClutterBuildGate.cs",
            "P1ProductionWorldMaterialDependencyGate.cs",
        )
        for filename in gate_files:
            with self.subTest(filename=filename):
                text = self.read(EDITOR / filename)
                self.assertIn(
                    "AfareetBuildContext.IsDedicatedExperimentalAndroidBuild(report)",
                    text,
                )
                self.assertIn("productionEvidence=false", text)

    def test_production_entry_point_remains_fail_closed(self):
        text = self.read(EDITOR / "AfareetBuild.cs")
        self.assertIn("public static void BuildAndroid()", text)
        self.assertIn("P1ProductionWorldBuildGate.ValidateAndroidCandidateOrThrow();", text)
        self.assertIn("public static void BuildAndroidExperimental()", text)
        self.assertIn("BuildOptions.Development", text)
        self.assertIn('new[] { "AFAREET_EXPERIMENTAL_APK" }', text)

    def test_experimental_runtime_keeps_hero_and_rivals_visible_without_promoting_art(self):
        hero = self.read(VEHICLE / "HeroCarProductionVisualInstaller.cs")
        rivals = self.read(VEHICLE / "RivalVariantPass.cs")

        self.assertIn("#if AFAREET_EXPERIMENTAL_APK", hero)
        self.assertIn("AFAREET_HERO_EXPERIMENTAL_BLOCKOUT_FALLBACK_ACTIVE", hero)
        self.assertIn("AFAREET_HERO_PRODUCTION_REQUIRED", hero)

        self.assertIn("UNITY_EDITOR || AFAREET_EXPERIMENTAL_APK", rivals)
        self.assertIn("AFAREET_UART004_EXPERIMENTAL_BLOCKOUT_RIVAL_ACTIVE", rivals)
        self.assertIn("AFAREET_UART004_PRODUCTION_RIVAL_REQUIRED", rivals)

    def test_gameplay_layout_guard_remains_active_for_experimental_apk(self):
        text = self.read(EDITOR / "CairoVerticalSliceLayoutBuildGate.cs")
        self.assertNotIn("IsDedicatedExperimentalAndroidBuild", text)
        self.assertIn("AFAREET_URAC011_VERTICAL_SLICE_GATE_OK", text)

    def test_content_stagers_remain_active_for_experimental_apk(self):
        for filename in (
            "P1ProductionLandmarkBuildPreprocessor.cs",
            "P1ProductionTrackDressingBuildPreprocessor.cs",
        ):
            with self.subTest(filename=filename):
                text = self.read(EDITOR / filename)
                self.assertNotIn("IsDedicatedExperimentalAndroidBuild", text)
                self.assertIn("StageTrackedSourcesOrThrow", text)


if __name__ == "__main__":
    unittest.main()
