import importlib.util
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "verify_unity_test_results.py"
SPEC = importlib.util.spec_from_file_location("verify_unity_test_results", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
VERIFY = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(VERIFY)

TestEvidenceError = VERIFY.TestEvidenceError
verify_artifact_tree = VERIFY.verify_artifact_tree


def write_report(path: Path, *, total=3, passed=3, failed=0, skipped=0, inconclusive=0, result="Passed"):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        f'<test-run total="{total}" passed="{passed}" failed="{failed}" skipped="{skipped}" inconclusive="{inconclusive}" result="{result}" />',
        encoding="utf-8",
    )


class VerifyUnityTestResultsTests(unittest.TestCase):
    def test_accepts_nested_passing_nunit_report(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_report(root / "editmode" / "results.xml", total=4, passed=3, skipped=1)
            reports = verify_artifact_tree(root)
            self.assertEqual(len(reports), 1)
            self.assertEqual(reports[0]["passed"], 3)

    def test_rejects_missing_xml(self):
        with tempfile.TemporaryDirectory() as tmp:
            with self.assertRaisesRegex(TestEvidenceError, "No XML test results"):
                verify_artifact_tree(Path(tmp))

    def test_rejects_all_skipped_report(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_report(root / "playmode" / "results.xml", total=5, passed=0, skipped=5)
            with self.assertRaisesRegex(TestEvidenceError, "no passing tests"):
                verify_artifact_tree(root)

    def test_rejects_failed_report(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_report(root / "editmode" / "results.xml", total=4, passed=3, failed=1, result="Failed")
            with self.assertRaisesRegex(TestEvidenceError, "failed tests"):
                verify_artifact_tree(root)

    def test_rejects_inconclusive_report(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_report(root / "playmode" / "results.xml", total=5, passed=4, inconclusive=1)
            with self.assertRaisesRegex(TestEvidenceError, "inconclusive tests"):
                verify_artifact_tree(root)

    def test_rejects_unaccounted_test_counter(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_report(root / "editmode" / "results.xml", total=4, passed=3)
            with self.assertRaisesRegex(TestEvidenceError, "do not account for every test"):
                verify_artifact_tree(root)

    def test_ignores_non_test_xml_but_requires_nunit_report(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "other.xml").write_text("<coverage />", encoding="utf-8")
            with self.assertRaisesRegex(TestEvidenceError, "No NUnit test-run XML"):
                verify_artifact_tree(root)


if __name__ == "__main__":
    unittest.main()
