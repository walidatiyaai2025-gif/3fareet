from pathlib import Path
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / "run_local_candidate_windows.ps1"


class WindowsPythonDiscoveryContractTests(unittest.TestCase):
    def test_candidate_orchestrator_probes_real_python3_before_use(self):
        text = SCRIPT.read_text(encoding="utf-8")
        required = [
            "[string]$PythonPath = \"\"",
            "function Test-Python3Candidate",
            "function Resolve-Python3",
            "Get-Command py -ErrorAction SilentlyContinue",
            "Get-Command python3 -ErrorAction SilentlyContinue",
            "Get-Command python -ErrorAction SilentlyContinue",
            "Start-Process",
            "'--version'",
            "-match '^Python 3\\.'",
            "Windows Store App Execution Alias python.exe is not sufficient",
            "AFAREET_PYTHON_RESOLVED",
            "& $pythonExecutable @pythonArgs @textNormalizeArgs",
            "& $pythonExecutable @pythonArgs @packageVerifyArgs",
            "& $pythonExecutable @pythonArgs @verifyArgs",
        ]
        for marker in required:
            self.assertIn(marker, text, marker)

    def test_py_launcher_is_tried_before_store_alias_prone_python(self):
        text = SCRIPT.read_text(encoding="utf-8")
        py_index = text.index("Get-Command py -ErrorAction SilentlyContinue")
        python3_index = text.index("Get-Command python3 -ErrorAction SilentlyContinue")
        python_index = text.index("Get-Command python -ErrorAction SilentlyContinue")
        self.assertLess(py_index, python3_index)
        self.assertLess(python3_index, python_index)

    def test_initial_dirty_evidence_is_preserved_before_python_resolution(self):
        text = SCRIPT.read_text(encoding="utf-8")
        preserve = text.index(
            'Preserve-DirtyTreeEvidence -Phase "INITIAL_TREE" -Changes $initialDirty'
        )
        refusal = text.index(
            "Candidate orchestration requires a clean Git working tree before Unity starts."
        )
        stale_purge = text.index("Clear-StaleCandidateEvidence\n")
        python_resolution = text.index("$python = Resolve-Python3 -RequestedPath $PythonPath")
        text_preflight = text.index("AFAREET_TEXT_NORMALIZATION_PREFLIGHT_START")

        self.assertLess(preserve, refusal)
        self.assertLess(refusal, stale_purge)
        self.assertLess(stale_purge, python_resolution)
        self.assertLess(python_resolution, text_preflight)


if __name__ == "__main__":
    unittest.main()
