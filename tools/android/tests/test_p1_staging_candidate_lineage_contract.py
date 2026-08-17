import json
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


REPO = Path(__file__).resolve().parents[3]
TOOLS = REPO / "tools" / "android"
VERIFIER = TOOLS / "verify_p1_staging_lineage_windows.ps1"
RUNNER = TOOLS / "run_p1_staged_candidate_windows.ps1"
TASKS = ["UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"]


def git(root: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", "-C", str(root), *args],
        check=check,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )


def make_repo() -> Path:
    root = Path(tempfile.mkdtemp(prefix="afareet-p1-lineage-repo-"))
    git(root, "init")
    git(root, "config", "user.email", "qa@example.invalid")
    git(root, "config", "user.name", "P1 QA")
    seed = root / "unity_game/Assets/Afareet/seed.txt"
    seed.parent.mkdir(parents=True, exist_ok=True)
    seed.write_text("seed\n", encoding="utf-8")
    git(root, "add", ".")
    git(root, "commit", "-m", "staging source")
    return root


def head(root: Path) -> str:
    return git(root, "rev-parse", "HEAD").stdout.strip().lower()


def commit_asset_change(root: Path, name: str = "PF_Afareet_Production.prefab") -> str:
    target = root / "unity_game/Assets/Afareet/Resources/Art/Vehicles/HeroCar/Production" / name
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(f"asset:{name}\n", encoding="utf-8")
    git(root, "add", ".")
    git(root, "commit", "-m", f"stage {name}")
    return head(root)


def write_report(path: Path, source_sha: str) -> None:
    evidence = []
    states = {
        "UART-003": "LICENSED_UNITY_STAGE_AND_BIND_OK",
        "UART-004": "LICENSED_UNITY_STAGE_AND_BIND_OK",
        "UART-005": "LICENSED_UNITY_IMPORT_STAGE_OK",
        "UART-006": "LICENSED_UNITY_IMPORT_STAGE_OK",
        "UART-007": "LICENSED_UNITY_IMPORT_STAGE_OK",
        "URAC-011": "LICENSED_UNITY_TRACKED_LAYOUT_IMPORT_OK",
    }
    for task in TASKS:
        evidence.append(
            {
                "taskId": task,
                "state": states[task],
                "sourceEvidence": f"source:{task}",
                "runtimeEvidence": f"runtime:{task}",
                "verified": False,
                "runtimeVerified": False,
                "ownerAccepted": False,
            }
        )
    payload = {
        "schemaVersion": 2,
        "state": "STAGED_FOR_COMMIT_NOT_CANDIDATE",
        "gitSha": source_sha,
        "heroSource": "Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing.fbx",
        "heroSourceGuid": "a" * 32,
        "heroPrefabGuid": "b" * 32,
        "coveredTasks": TASKS,
        "taskEvidence": evidence,
        "verified": False,
        "runtimeVerified": False,
        "ownerAccepted": False,
        "publicationEligible": False,
        "candidateBuildStarted": False,
    }
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def invoke(root: Path, report: Path, output: Path | None = None) -> subprocess.CompletedProcess[str]:
    pwsh = shutil.which("pwsh")
    if not pwsh:
        raise unittest.SkipTest("pwsh is not installed")
    args = [
        pwsh,
        "-NoProfile",
        "-File",
        str(VERIFIER),
        "-RepoRoot",
        str(root),
        "-StagingReport",
        str(report),
    ]
    if output is not None:
        args.extend(["-Output", str(output)])
    return subprocess.run(args, capture_output=True, text=True)


class P1StagingCandidateLineageTests(unittest.TestCase):
    def test_native_scripts_parse_when_pwsh_is_available(self):
        pwsh = shutil.which("pwsh")
        if not pwsh:
            self.skipTest("pwsh is not installed")
        for script in (VERIFIER, RUNNER):
            command = (
                "$tokens=$null; $errors=$null; "
                f"[System.Management.Automation.Language.Parser]::ParseFile('{script.as_posix()}', "
                "[ref]$tokens, [ref]$errors) | Out-Null; "
                "if ($errors.Count -gt 0) { $errors | ForEach-Object { Write-Error $_.Message }; exit 1 }"
            )
            result = subprocess.run([pwsh, "-NoProfile", "-Command", command], capture_output=True, text=True)
            self.assertEqual(0, result.returncode, msg=f"{script.name}\n{result.stdout}{result.stderr}")

    def test_direct_child_asset_only_staging_commit_is_bound(self):
        root = make_repo()
        report_dir = Path(tempfile.mkdtemp(prefix="afareet-p1-lineage-report-"))
        try:
            source_sha = head(root)
            candidate_sha = commit_asset_change(root)
            report = report_dir / "p1-staging-handoff.json"
            write_report(report, source_sha)
            output = root / "artifacts/p1-lineage.json"
            result = invoke(root, report, output)
            self.assertEqual(0, result.returncode, msg=result.stdout + result.stderr)
            payload = json.loads(output.read_text(encoding="utf-8-sig"))
            self.assertEqual("STAGING_PARENT_BOUND_TO_CANDIDATE", payload["state"])
            self.assertEqual(source_sha, payload["stagingSourceGitSha"])
            self.assertEqual(candidate_sha, payload["candidateGitSha"])
            self.assertEqual(source_sha, payload["directParentGitSha"])
            self.assertEqual(TASKS, payload["coveredTasks"])
            self.assertTrue(payload["readyForLicensedCandidateTests"])
            self.assertFalse(payload["verified"])
            self.assertFalse(payload["runtimeVerified"])
            self.assertFalse(payload["ownerAccepted"])
            self.assertFalse(payload["publicationEligible"])
            self.assertRegex(payload["stagingReportSha256"], r"^[0-9a-f]{64}$")
        finally:
            shutil.rmtree(root, ignore_errors=True)
            shutil.rmtree(report_dir, ignore_errors=True)

    def test_intervening_commit_breaks_direct_parent_lineage(self):
        root = make_repo()
        report_dir = Path(tempfile.mkdtemp(prefix="afareet-p1-lineage-report-"))
        try:
            source_sha = head(root)
            commit_asset_change(root, "first.prefab")
            commit_asset_change(root, "second.prefab")
            report = report_dir / "p1-staging-handoff.json"
            write_report(report, source_sha)
            result = invoke(root, report)
            self.assertNotEqual(0, result.returncode)
            diagnostic = (result.stdout + result.stderr).lower()
            self.assertIn("direct reviewed", diagnostic)
            self.assertIn("staging-output commit", diagnostic)
        finally:
            shutil.rmtree(root, ignore_errors=True)
            shutil.rmtree(report_dir, ignore_errors=True)

    def test_candidate_staging_commit_cannot_change_non_asset_path(self):
        root = make_repo()
        report_dir = Path(tempfile.mkdtemp(prefix="afareet-p1-lineage-report-"))
        try:
            source_sha = head(root)
            doc = root / "docs/unauthorized.md"
            doc.parent.mkdir(parents=True, exist_ok=True)
            doc.write_text("not staging output\n", encoding="utf-8")
            git(root, "add", ".")
            git(root, "commit", "-m", "wrong staging commit")
            report = report_dir / "p1-staging-handoff.json"
            write_report(report, source_sha)
            result = invoke(root, report)
            self.assertNotEqual(0, result.returncode)
            self.assertIn("outside unity_game/Assets/", result.stdout + result.stderr)
        finally:
            shutil.rmtree(root, ignore_errors=True)
            shutil.rmtree(report_dir, ignore_errors=True)

    def test_p1_runner_enforces_lineage_before_generic_unity_candidate(self):
        text = RUNNER.read_text(encoding="utf-8")
        required = (
            "verify_p1_staging_lineage_windows.ps1",
            "run_local_candidate_windows.ps1",
            "AFAREET_P1_STAGED_CANDIDATE_LINEAGE_START",
            "AFAREET_P1_STAGED_CANDIDATE_LINEAGE_OK",
            "STAGING_PARENT_BOUND_TO_CANDIDATE",
            "readyForLicensedCandidateTests",
            "p1-staged-candidate-manifest.json",
            "p1-staged-local-windows-licensed-unity",
            "P1_STAGED_CANDIDATE_READY_FOR_PHYSICAL_DEVICE_EVIDENCE",
            "stagingReportSha256",
            "localCandidateManifest",
            "apkSha256",
            "verified = $false",
            "runtimeVerified = $false",
            "ownerAccepted = $false",
            "publicationEligible = $false",
        )
        for marker in required:
            self.assertIn(marker, text)
        self.assertLess(text.index("& $lineageVerifier"), text.index("& $genericRunner"))
        self.assertNotIn("git commit", text)
        self.assertNotIn("git add", text)


if __name__ == "__main__":
    unittest.main()
