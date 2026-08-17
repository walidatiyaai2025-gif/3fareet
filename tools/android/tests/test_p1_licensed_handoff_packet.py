import hashlib
import importlib.util
import json
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
TOOLS = ROOT / "tools" / "android"


def load_module():
    path = TOOLS / "p1_licensed_handoff_packet.py"
    spec = importlib.util.spec_from_file_location("p1_licensed_handoff_packet_under_test", path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


MODULE = load_module()
EXPECTED_TASKS = ["UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"]


class P1LicensedHandoffPacketTests(unittest.TestCase):
    def test_current_repo_reports_five_of_six_source_ready_and_external_hero_blocker(self):
        packet = MODULE.build_packet(ROOT)
        self.assertEqual("BLOCKED_EXTERNAL_HERO_SOURCE", packet["state"])
        self.assertEqual(5, packet["visualSourceSummary"]["sourceReadyCount"])
        self.assertEqual(1, packet["visualSourceSummary"]["blockedCount"])
        self.assertEqual(["UART-003"], packet["visualSourceSummary"]["blockedTaskIds"])
        tasks = packet["visualSourceSummary"]["tasks"]
        self.assertEqual(EXPECTED_TASKS, [item["taskId"] for item in tasks])
        self.assertFalse(tasks[0]["sourceReady"])
        self.assertTrue(all(item["sourceReady"] for item in tasks[1:]))
        self.assertIn("Afareet King", packet["nextAction"])

    def test_packet_binds_exact_operator_chain_and_next_licensed_stage_order(self):
        packet = MODULE.build_packet(ROOT)
        chain_path = TOOLS / "p1_operator_release_chain.json"
        expected_hash = hashlib.sha256(chain_path.read_bytes()).hexdigest()
        self.assertEqual(expected_hash, packet["operatorChain"]["sha256"])
        self.assertEqual(13, packet["operatorChain"]["stageCount"])
        self.assertTrue(packet["operatorChain"]["authoritativeForP1"])
        self.assertEqual(
            [
                "UART003_NATIVE_WINDOWS_INTAKE",
                "P1_LICENSED_UNITY_STAGING",
                "P1_REVIEW_AND_COMMIT_STAGING_DELTA",
                "P1_STAGED_CANDIDATE",
            ],
            [item["id"] for item in packet["nextLicensedStages"]],
        )

    def test_missing_hero_commands_use_explicit_placeholder_and_never_publish(self):
        packet = MODULE.build_packet(ROOT)
        commands = packet["commands"]
        self.assertTrue(commands["heroSourcePlaceholder"])
        self.assertIsNone(commands["heroSourceUnityPath"])
        self.assertIn("<REAL_HERO_SOURCE.fbx>", commands["nativeHeroIntake"])
        self.assertIn("validate_hero_asset_intake_windows.ps1", commands["nativeHeroIntake"])
        self.assertIn("stage_production_candidate_windows.ps1", commands["licensedUnityStaging"])
        command_text = "\n".join(str(value) for value in commands.values())
        self.assertNotIn("git push", command_text.lower())
        self.assertNotIn("git tag", command_text.lower())
        self.assertNotIn("gh release", command_text.lower())

    def test_packet_never_self_asserts_execution_approval_publication_or_verification(self):
        packet = MODULE.build_packet(ROOT)
        for key in (
            "licensedUnityExecuted",
            "candidateBuildStarted",
            "physicalDeviceEvidenceCaptured",
            "humanApprovalRecorded",
            "publicationEligible",
            "publicationPerformed",
            "verified",
            "runtimeVerified",
            "ownerAccepted",
        ):
            self.assertIs(packet[key], False, key)
        for task in packet["visualSourceSummary"]["tasks"]:
            self.assertIs(task["verified"], False)
            self.assertIs(task["runtimeVerified"], False)
            self.assertIs(task["ownerAccepted"], False)

    def test_output_is_artifacts_only_and_refuses_overwrite(self):
        packet = MODULE.build_packet(ROOT)
        with tempfile.TemporaryDirectory(dir=ROOT / "artifacts") as tmp:
            output = Path(tmp) / "handoff.json"
            MODULE.write_packet(ROOT, output, packet)
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(packet["state"], payload["state"])
            with self.assertRaisesRegex(MODULE.P1LicensedHandoffPacketError, "refusing to overwrite"):
                MODULE.write_packet(ROOT, output, packet)

        with tempfile.TemporaryDirectory() as tmp:
            outside = Path(tmp) / "handoff.json"
            with self.assertRaisesRegex(MODULE.P1LicensedHandoffPacketError, "under <repo>/artifacts"):
                MODULE.write_packet(ROOT, outside, packet)

    def test_cli_blocked_packet_requires_explicit_allow_blocked(self):
        self.assertEqual(3, MODULE.main(["--repo-root", str(ROOT)]))
        self.assertEqual(0, MODULE.main(["--repo-root", str(ROOT), "--allow-blocked"]))

    def test_live_windows_staging_git_checks_are_strictmode_safe(self):
        script_path = TOOLS / "stage_production_candidate_windows.ps1"
        text = script_path.read_text(encoding="utf-8")
        self.assertIn("Set-StrictMode -Version Latest", text)
        self.assertNotIn("$LASTEXITCODE", text)
        for marker in (
            "$gitTopSucceeded = $?",
            "$gitShaSucceeded = $?",
            "$initialStatusSucceeded = $?",
            "$heroTrackedByGit = $?",
            "$postStageStatusSucceeded = $?",
        ):
            self.assertIn(marker, text)

    def test_windows_staging_script_parses_when_pwsh_is_available(self):
        pwsh = shutil.which("pwsh") or shutil.which("powershell")
        if not pwsh:
            self.skipTest("PowerShell is not installed")
        script = TOOLS / "stage_production_candidate_windows.ps1"
        command = (
            "$tokens=$null;$errors=$null;"
            f"[System.Management.Automation.Language.Parser]::ParseFile('{script.as_posix()}',[ref]$tokens,[ref]$errors)|Out-Null;"
            "if($errors.Count -gt 0){$errors|ForEach-Object{Write-Error $_.Message};exit 1}"
        )
        completed = subprocess.run([pwsh, "-NoProfile", "-Command", command], capture_output=True, text=True, check=False)
        self.assertEqual(0, completed.returncode, completed.stderr or completed.stdout)


if __name__ == "__main__":
    unittest.main()
