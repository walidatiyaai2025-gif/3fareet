import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
GATE_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Editor/CairoVerticalSliceLayoutBuildGate.cs"


class CairoVerticalSliceBuildGateUnity6ContractTests(unittest.TestCase):
    def test_runtime_segment_guard_uses_authored_document_values_not_constant_false_branch(self):
        source = GATE_PATH.read_text(encoding="utf-8")
        self.assertIn(
            "if (document.points.Length * document.samplesPerControlPoint != RequiredRuntimeSegments)",
            source,
        )
        self.assertIn(
            '$"runtimeSegments={document.points.Length * document.samplesPerControlPoint} "',
            source,
        )
        self.assertNotIn(
            "if (RequiredControlPoints * SamplesPerControlPoint != RequiredRuntimeSegments)",
            source,
        )
        self.assertNotIn("internal-runtime-segment-contract-invalid", source)


if __name__ == "__main__":
    unittest.main()
