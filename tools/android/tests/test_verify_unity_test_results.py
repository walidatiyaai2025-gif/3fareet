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


def write_problem_report(path: Path, *, case_count=1, case_result="Failed"):
    path.parent.mkdir(parents=True, exist_ok=True)
    failed = case_count if case_result == "Failed" else 0
    inconclusive = case_count if case_result == "Inconclusive" else 0
    cases = "".join(
        f'''<test-case name="Case{i}" fullname="Afareet.Tests.Case{i}" result="{case_result}">
          <failure>
            <message>expected lane {i}\nactual lane {i + 1}</message>
            <stack-trace>at Afareet.Tests.Case{i}()\nat Other.Frame()</stack-trace>
          </failure>
        </test-case>'''
        for i in range(case_count)
    )
    path.write_text(
        f'''<test-run total="{case_count + 1}" passed="1" failed="{failed}" skipped="0" inconclusive="{inconclusive}" result="{case_result}">
          <test-suite>{cases}</test-suite>
        </test-run>''',
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

    def test_failed_report_surfaces_test_name_message_and_stack(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_problem_report(root / "editmode" / "results.xml")
            with self.assertRaises(TestEvidenceError) as captured:
                verify_artifact_tree(root)
            message = str(captured.exception)
            self.assertIn("Afareet.Tests.Case0", message)
            self.assertIn("expected lane 0 | actual lane 1", message)
            self.assertIn("at Afareet.Tests.Case0() | at Other.Frame()", message)

    def test_problem_diagnostics_are_bounded_to_25_cases(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_problem_report(root / "editmode" / "results.xml", case_count=27)
            with self.assertRaises(TestEvidenceError) as captured:
                verify_artifact_tree(root)
            message = str(captured.exception)
            self.assertIn("Afareet.Tests.Case24", message)
            self.assertNotIn("Afareet.Tests.Case25", message)
            self.assertIn("... 2 more problem test case(s)", message)

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
