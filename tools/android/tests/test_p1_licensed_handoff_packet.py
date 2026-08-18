import hashlib
import importlib.util
import json
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


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
SHA_A = "a" * 40
SHA_B = "b" * 40


def current_head() -> str:
    result = subprocess.run(
        ["git", "-C", str(ROOT), "rev-parse", "HEAD"],
        capture_output=True,
        text=True,
        check=True,
    )
    return result.stdout.strip().lower()


def ready_visual():
    return {
        "state": "READY_FOR_LICENSED_VISUAL_STAGING",
        "sourceReadyCount": 6,
        "blockedCount": 0,
        "blockedTaskIds": [],
        "tasks": [
            {
                "taskId": task_id,
                "state": "SOURCE_READY_FOR_LICENSED_RUNTIME_PROOF",
                "sourceReady": True,
                "blockedCheckIds": [],
            }
            for task_id in EXPECTED_TASKS
        ],
    }


def ready_staging(git_sha: str):
    return {
        "state": "READY_FOR_LICENSED_STAGING",
        "gitSha": git_sha,
        "readyForLicensedStaging": True,
        "blockedCheckIds": [],
        "nextAction": "Run licensed staging.",
    }


class P1LicensedHandoffPacketTests(unittest.TestCase):
    def test_current_repo_reports_four_of_six_source_ready_and_external_visual_blockers(self):
        packet = MODULE.build_packet(ROOT, expected_git_sha=current_head(), environment={})
        self.assertEqual("BLOCKED_EXTERNAL_VISUAL_SOURCES", packet["state"])
        self.assertEqual(4, packet["visualSourceSummary"]["sourceReadyCount"])
        self.assertEqual(2, packet["visualSourceSummary"]["blockedCount"])
        self.assertEqual(["UART-003", "UART-004"], packet["visualSourceSummary"]["blockedTaskIds"])
        tasks = packet["visualSourceSummary"]["tasks"]
        self.assertEqual(EXPECTED_TASKS, [item["taskId"] for item in tasks])
        self.assertFalse(tasks[0]["sourceReady"])
        self.assertFalse(tasks[1]["sourceReady"])
        self.assertTrue(all(item["sourceReady"] for item in tasks[2:]))
        self.assertTrue(packet["gitIdentity"]["exactSourceIdentitySatisfied"])
        self.assertFalse(packet["releaseHandoffEligible"])
        self.assertIn("UART-003", packet["nextAction"])
        self.assertIn("UART-004", packet["nextAction"])
        self.assertIn("externally-authored production visual source", packet["nextAction"])

    def test_packet_binds_exact_operator_chain_and_next_licensed_stage_order(self):
        packet = MODULE.build_packet(ROOT, expected_git_sha=current_head(), environment={})
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
        self.assertEqual(
            "tools/android/run_p1_licensed_staging_windows.ps1",
            packet["nextLicensedStages"][1]["tool"],
        )

    def test_missing_hero_commands_use_explicit_placeholders_and_never_publish(self):
        packet = MODULE.build_packet(ROOT, environment={})
        commands = packet["commands"]
        self.assertTrue(commands["heroSourcePlaceholder"])
        self.assertTrue(commands["expectedGitShaPlaceholder"])
        self.assertIsNone(commands["heroSourceUnityPath"])
        self.assertIsNone(commands["expectedGitSha"])
        self.assertIn("<REAL_HERO_SOURCE.fbx>", commands["nativeHeroIntake"])
        self.assertIn("<EXACT_SOURCE_GIT_SHA>", commands["portableAudit"])
        self.assertIn("validate_hero_asset_intake_windows.ps1", commands["nativeHeroIntake"])
        self.assertIn("run_p1_licensed_staging_windows.ps1", commands["licensedUnityStaging"])
        self.assertIn("-HandoffPacket artifacts/production-staging/p1-licensed-handoff-packet.json", commands["licensedUnityStaging"])
        self.assertNotIn("stage_production_candidate_windows.ps1", commands["licensedUnityStaging"])
        command_text = "\n".join(str(value) for value in commands.values())
        self.assertNotIn("git push", command_text.lower())
        self.assertNotIn("git tag", command_text.lower())
        self.assertNotIn("gh release", command_text.lower())

    def test_packet_never_self_asserts_execution_approval_publication_or_verification(self):
        packet = MODULE.build_packet(ROOT, expected_git_sha=current_head(), environment={})
        self.assertFalse(packet["releaseHandoffEligible"])
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

    def test_exact_source_identity_is_required_for_ready_state(self):
        with mock.patch.object(MODULE.p1_visual_source_readiness, "audit_visual_sources", return_value=ready_visual()), mock.patch.object(
            MODULE.p1_licensed_staging_readiness,
            "audit",
            return_value=ready_staging(SHA_A),
        ):
            missing = MODULE.build_packet(
                ROOT,
                hero_source="Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing.fbx",
                environment={},
            )
            self.assertEqual("BLOCKED_GIT_IDENTITY", missing["state"])
            self.assertEqual("EXPECTED_SOURCE_SHA_REQUIRED", missing["gitIdentity"]["status"])
            self.assertFalse(missing["releaseHandoffEligible"])

            mismatch = MODULE.build_packet(
                ROOT,
                hero_source="Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing.fbx",
                expected_git_sha=SHA_B,
                environment={},
            )
            self.assertEqual("BLOCKED_GIT_IDENTITY", mismatch["state"])
            self.assertEqual("EXPECTED_SOURCE_SHA_MISMATCH", mismatch["gitIdentity"]["status"])
            self.assertFalse(mismatch["gitIdentity"]["gitIdentityMatched"])

            exact = MODULE.build_packet(
                ROOT,
                hero_source="Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing.fbx",
                expected_git_sha=SHA_A.upper(),
                environment={},
            )
            self.assertEqual("READY_FOR_LICENSED_OPERATOR_HANDOFF", exact["state"])
            self.assertEqual("EXACT_SOURCE_SHA", exact["gitIdentity"]["status"])
            self.assertTrue(exact["gitIdentity"]["gitIdentityMatched"])
            self.assertTrue(exact["gitIdentity"]["exactSourceIdentitySatisfied"])
            self.assertTrue(exact["releaseHandoffEligible"])

    def test_synthetic_pr_merge_ref_is_never_release_handoff_eligible(self):
        environment = {
            "GITHUB_EVENT_NAME": "pull_request",
            "GITHUB_REF": "refs/pull/217/merge",
            "GITHUB_HEAD_REF": "agent/step18-handoff-sha-identity",
            "GITHUB_SHA": SHA_A,
        }
        with mock.patch.object(MODULE.p1_visual_source_readiness, "audit_visual_sources", return_value=ready_visual()), mock.patch.object(
            MODULE.p1_licensed_staging_readiness,
            "audit",
            return_value=ready_staging(SHA_A),
        ):
            packet = MODULE.build_packet(
                ROOT,
                hero_source="Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing.fbx",
                expected_git_sha=SHA_B,
                environment=environment,
            )
        identity = packet["gitIdentity"]
        self.assertEqual("BLOCKED_GIT_IDENTITY", packet["state"])
        self.assertEqual("SYNTHETIC_PR_MERGE_REF", identity["status"])
        self.assertTrue(identity["syntheticPullRequestMerge"])
        self.assertFalse(identity["gitIdentityMatched"])
        self.assertFalse(identity["exactSourceIdentitySatisfied"])
        self.assertFalse(packet["releaseHandoffEligible"])
        self.assertIn("synthetic pull-request merge refs", packet["nextAction"])

    def test_invalid_expected_source_sha_fails_closed(self):
        with self.assertRaisesRegex(MODULE.P1LicensedHandoffPacketError, "full 40-character"):
            MODULE.build_packet(ROOT, expected_git_sha="deadbeef", environment={})

    def test_output_is_artifacts_only_and_refuses_overwrite(self):
        packet = MODULE.build_packet(ROOT, expected_git_sha=current_head(), environment={})
        artifact_root = ROOT / "artifacts"
        artifact_root.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(dir=artifact_root) as tmp:
            output = Path(tmp) / "handoff.json"
            MODULE.write_packet(ROOT, output, packet)
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(packet["state"], payload["state"])
            self.assertEqual(2, payload["schemaVersion"])
            with self.assertRaisesRegex(MODULE.P1LicensedHandoffPacketError, "refusing to overwrite"):
                MODULE.write_packet(ROOT, output, packet)

        with tempfile.TemporaryDirectory() as tmp:
            outside = Path(tmp) / "handoff.json"
            with self.assertRaisesRegex(MODULE.P1LicensedHandoffPacketError, "under <repo>/artifacts"):
                MODULE.write_packet(ROOT, outside, packet)

    def test_cli_blocked_packet_requires_explicit_allow_blocked(self):
        sha = current_head()
        self.assertEqual(3, MODULE.main(["--repo-root", str(ROOT), "--expected-git-sha", sha]))
        self.assertEqual(0, MODULE.main(["--repo-root", str(ROOT), "--expected-git-sha", sha, "--allow-blocked"]))

    def test_live_windows_staging_git_checks_are_strictmode_safe(self):
        script_path = TOOLS / "stage_production_candidate_windows.ps1"
        text = script_path.read_text(encoding="utf-8")
        self.assertIn("Set-StrictMode -Version Latest", text)
        self.assertNotIn("$LASTEXITCODE", text)
        for marker in (
            "$gitTopSucceeded = $?",
            "$gitShaSucceeded = $?",
            "$initialStatusSucceeded = $?",
            "$postStageStatusSucceeded = $?",
            "function Assert-TrackedNonEmptyFile",
            "ls-files --error-unmatch -- $normalized",
            "if (-not $?)",
            "$heroBytes = Assert-TrackedNonEmptyFile",
            "$rivalBytes = Assert-TrackedNonEmptyFile",
            "AFAREET_STAGING_EXTERNAL_SOURCE_PREFLIGHT_OK",
        ):
            self.assertIn(marker, text)

    def test_windows_staging_script_parses_when_pwsh_is_available(self):
        pwsh = shutil.which("pwsh") or shutil.which("powershell")
        if not pwsh:
            self.skipTest("PowerShell is not installed")
        for script in (
            TOOLS / "verify_p1_licensed_handoff_packet_windows.ps1",
            TOOLS / "run_p1_licensed_staging_windows.ps1",
            TOOLS / "stage_production_candidate_windows.ps1",
        ):
            command = (
                "$tokens=$null;$errors=$null;"
                f"[System.Management.Automation.Language.Parser]::ParseFile('{script.as_posix()}',[ref]$tokens,[ref]$errors)|Out-Null;"
                "if($errors.Count -gt 0){$errors|ForEach-Object{Write-Error $_.Message};exit 1}"
            )
            completed = subprocess.run([pwsh, "-NoProfile", "-Command", command], capture_output=True, text=True, check=False)
            self.assertEqual(0, completed.returncode, completed.stderr or completed.stdout)


if __name__ == "__main__":
    unittest.main()
