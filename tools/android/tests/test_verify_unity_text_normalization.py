import importlib.util
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "verify_unity_text_normalization.py"
SPEC = importlib.util.spec_from_file_location("verify_unity_text_normalization", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class UnityTextNormalizationTests(unittest.TestCase):
    def _repo(self):
        temp = tempfile.TemporaryDirectory()
        root = Path(temp.name)
        subprocess.run(["git", "init", "-q", str(root)], check=True)
        return temp, root

    def _write_contract_tree(self, root: Path, *, crlf: bool = False, include_attributes: bool = True):
        if include_attributes:
            (root / ".gitattributes").write_text(
                "unity_game/ProjectSettings/*.asset text eol=lf\n"
                "unity_game/ProjectSettings/*.txt text eol=lf\n"
                "unity_game/Packages/*.json text eol=lf\n",
                encoding="utf-8",
            )

        project_settings = root / "unity_game" / "ProjectSettings"
        packages = root / "unity_game" / "Packages"
        project_settings.mkdir(parents=True)
        packages.mkdir(parents=True)

        newline = b"\r\n" if crlf else b"\n"
        (project_settings / "TimeManager.asset").write_bytes(b"TimeManager:" + newline)
        (project_settings / "ProjectVersion.txt").write_bytes(b"m_EditorVersion: 6000.5.8f1" + newline)
        (packages / "manifest.json").write_bytes(b'{"dependencies": {}}' + newline)
        (packages / "packages-lock.json").write_bytes(b'{"dependencies": {}}' + newline)

        subprocess.run(["git", "-C", str(root), "add", "."], check=True)

    def test_accepts_tracked_lf_files_with_explicit_attributes(self):
        temp, root = self._repo()
        self.addCleanup(temp.cleanup)
        self._write_contract_tree(root)

        checked = MODULE.verify(root)

        self.assertEqual(4, len(checked))
        self.assertIn("unity_game/ProjectSettings/TimeManager.asset", checked)
        self.assertIn("unity_game/Packages/packages-lock.json", checked)

    def test_rejects_missing_text_attributes(self):
        temp, root = self._repo()
        self.addCleanup(temp.cleanup)
        self._write_contract_tree(root, include_attributes=False)

        with self.assertRaisesRegex(MODULE.TextNormalizationError, "expected 'set'"):
            MODULE.verify(root)

    def test_rejects_crlf_working_tree_bytes_even_when_attributes_are_lf(self):
        temp, root = self._repo()
        self.addCleanup(temp.cleanup)
        self._write_contract_tree(root, crlf=True)

        with self.assertRaisesRegex(MODULE.TextNormalizationError, "CRLF bytes"):
            MODULE.verify(root)


if __name__ == "__main__":
    unittest.main()
