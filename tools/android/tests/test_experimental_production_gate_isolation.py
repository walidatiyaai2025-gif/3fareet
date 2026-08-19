import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
EDITOR = ROOT / "unity_game/Assets/Afareet/Editor"
BUILD = EDITOR / "AfareetBuild.cs"
CONTEXT = EDITOR / "AfareetBuildContext.cs"
GATES = [
    EDITOR / "RivalProductionBuildGate.cs",
    EDITOR / "P1ProductionLandmarkBuildGate.cs",
    EDITOR / "P1ProductionTrackDressingBuildGate.cs",
]

class ExperimentalGateIsolationContract(unittest.TestCase):
    def test_build_scope_exists_and_resets(self):
        build = BUILD.read_text(encoding="utf-8-sig")
        context = CONTEXT.read_text(encoding="utf-8-sig")
        self.assertIn("using (AfareetBuildContext.BeginExperimentalAndroidBuild())", build)
        self.assertIn("internal static bool IsExperimentalAndroidBuild", context)
        self.assertIn("IsExperimentalAndroidBuild = false;", context)

    def test_three_blocking_production_gates_are_experimental_only_bypassed(self):
        for path in GATES:
            source = path.read_text(encoding="utf-8-sig")
            guard = "if (AfareetBuildContext.IsExperimentalAndroidBuild)"
            self.assertIn(guard, source, msg=path.name)
            self.assertIn("productionEvidence=false", source, msg=path.name)
            self.assertIn("BuildTarget.Android", source, msg=path.name)

            guard_at = source.index(guard)
            if path.name == "RivalProductionBuildGate.cs":
                validation_at = source.index("RivalProductionPolicy.ValidateContract();")
            else:
                validation_at = source.index("ValidateAndroidCandidateOrThrow();", guard_at)
            self.assertLess(guard_at, validation_at, msg=path.name)

    def test_production_fail_closed_contract_remains(self):
        rival = (EDITOR / "RivalProductionBuildGate.cs").read_text(encoding="utf-8-sig")
        landmark = (EDITOR / "P1ProductionLandmarkBuildGate.cs").read_text(encoding="utf-8-sig")
        dressing = (EDITOR / "P1ProductionTrackDressingBuildGate.cs").read_text(encoding="utf-8-sig")
        self.assertIn("missing-external-authored-prefab", rival)
        self.assertIn('ProductionReadyState = "PRODUCTION_READY"', landmark)
        self.assertIn('ProductionReadyState = "PRODUCTION_READY"', dressing)

if __name__ == "__main__":
    unittest.main()
