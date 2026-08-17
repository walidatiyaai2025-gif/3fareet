import hashlib
import json
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
TOOLS = ROOT / "tools" / "android"
VERIFY_SCRIPT = TOOLS / "verify_p1_licensed_handoff_packet_windows.ps1"
RUNNER_SCRIPT = TOOLS / "run_p1_licensed_staging_windows.ps1"
CHAIN_SOURCE = TOOLS / "p1_operator_release_chain.json"
EXPECTED_TASKS = ["UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"]


def pwsh_path():
    return shutil.which("pwsh") or shutil.which("powershell")


def run(command, cwd=None):
    return subprocess.run(command, cwd=cwd, capture_output=True, text=True, check=False)


def git(root: Path, *args: str) -> str:
    completed = run(["git", "-C", str(root), *args])
    if completed.returncode != 0:
        raise AssertionError(completed.stderr or completed.stdout)
    return completed.stdout.strip()


def write_json(path: Path, payload) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def build_repo(root: Path) -> dict[str, Path | str]:
    git(root, "init")
    git(root, "config", "user.email", "tests@example.invalid")
    git(root, "config", "user.name", "P1 Native Handoff Test")
    (root / ".gitignore").write_text("artifacts/\n", encoding="utf-8")

    chain = root / "tools" / "android" / "p1_operator_release_chain.json"
    chain.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(CHAIN_SOURCE, chain)

    hero_repo = "unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing.fbx"
    hero = root / hero_repo
    hero.parent.mkdir(parents=True, exist_ok=True)
    hero.write_bytes(b"authored-hero-fixture-not-production-proof")

    git(root, "add", ".gitignore", "tools/android/p1_operator_release_chain.json", hero_repo)
    git(root, "commit", "-m", "fixture: exact source commit")
    sha = git(root, "rev-parse", "HEAD").lower()
    chain_sha = hashlib.sha256(chain.read_bytes()).hexdigest()

    packet = root / "artifacts" / "production-staging" / "p1-licensed-handoff-packet.json"
    packet_payload = {
        "schemaVersion": 2,
        "state": "READY_FOR_LICENSED_OPERATOR_HANDOFF",
        "gitSha": sha,
        "gitIdentity": {
            "status": "EXACT_SOURCE_SHA",
            "observedGitSha": sha,
            "expectedGitSha": sha,
            "gitIdentityMatched": True,
            "syntheticPullRequestMerge": False,
            "exactSourceIdentitySatisfied": True,
            "checkoutContext": {
                "githubEventName": None,
                "githubRef": None,
                "githubHeadRef": None,
                "githubSha": None,
            },
        },
        "releaseHandoffEligible": True,
        "expectedUnityVersion": "6000.5.8f1",
        "heroSource": hero_repo,
        "fixedRegisterSize": 65,
        "operatorChain": {
            "file": "tools/android/p1_operator_release_chain.json",
            "sha256": chain_sha,
            "stageCount": 13,
            "authoritativeForP1": True,
        },
        "visualSourceSummary": {
            "state": "READY_FOR_LICENSED_VISUAL_STAGING",
            "sourceReadyCount": 6,
            "blockedCount": 0,
            "blockedTaskIds": [],
            "tasks": [
                {
                    "taskId": task_id,
                    "sourceState": "SOURCE_READY_FOR_LICENSED_RUNTIME_PROOF",
                    "sourceReady": True,
                    "blockedCheckIds": [],
                    "verified": False,
                    "runtimeVerified": False,
                    "ownerAccepted": False,
                }
                for task_id in EXPECTED_TASKS
            ],
        },
        "licensedStagingSummary": {
            "state": "READY_FOR_LICENSED_STAGING",
            "readyForLicensedStaging": True,
            "blockedCheckIds": [],
        },
        "licensedUnityExecuted": False,
        "candidateBuildStarted": False,
        "physicalDeviceEvidenceCaptured": False,
        "humanApprovalRecorded": False,
        "publicationEligible": False,
        "publicationPerformed": False,
        "verified": False,
        "runtimeVerified": False,
        "ownerAccepted": False,
    }
    write_json(packet, packet_payload)
    return {"packet": packet, "hero": hero, "heroRepo": hero_repo, "sha": sha, "chain": chain}


class P1NativeHandoffEnforcementTests(unittest.TestCase):
    def setUp(self):
        if not pwsh_path():
            self.skipTest("PowerShell is not installed")

    def invoke_verifier(self, root: Path, packet: Path, hero_source: str, output: Path | None = None):
        command = [
            pwsh_path(),
            "-NoProfile",
            "-File",
            str(VERIFY_SCRIPT),
            "-Packet",
            str(packet),
            "-HeroSource",
            hero_source,
            "-RepoRoot",
            str(root),
        ]
        if output is not None:
            command.extend(["-Output", str(output)])
        return run(command)

    def test_native_scripts_parse(self):
        for script in (VERIFY_SCRIPT, RUNNER_SCRIPT):
            command = (
                "$tokens=$null;$errors=$null;"
                f"[System.Management.Automation.Language.Parser]::ParseFile('{script.as_posix()}',[ref]$tokens,[ref]$errors)|Out-Null;"
                "if($errors.Count -gt 0){$errors|ForEach-Object{Write-Error $_.Message};exit 1}"
            )
            completed = run([pwsh_path(), "-NoProfile", "-Command", command])
            self.assertEqual(0, completed.returncode, completed.stderr or completed.stdout)

    def test_exact_ready_packet_passes_and_output_never_claims_execution_or_verification(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            fixture = build_repo(root)
            output = root / "artifacts" / "production-staging" / "native-verified.json"
            completed = self.invoke_verifier(
                root,
                fixture["packet"],
                "Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing.fbx",
                output,
            )
            self.assertEqual(0, completed.returncode, completed.stderr or completed.stdout)
            self.assertIn("AFAREET_P1_NATIVE_HANDOFF_VERIFY_OK", completed.stdout)
            payload = json.loads(output.read_text(encoding="utf-8-sig"))
            self.assertEqual("NATIVE_P1_HANDOFF_VERIFIED_FOR_LICENSED_STAGING", payload["state"])
            self.assertEqual(fixture["sha"], payload["gitSha"])
            self.assertEqual(6, payload["sourceReadyCount"])
            self.assertTrue(payload["licensedStagingReady"])
            self.assertTrue(payload["releaseHandoffEligible"])
            for key in (
                "licensedUnityExecuted",
                "candidateBuildStarted",
                "publicationEligible",
                "publicationPerformed",
                "verified",
                "runtimeVerified",
                "ownerAccepted",
            ):
                self.assertFalse(payload[key], key)

    def test_git_identity_mismatch_and_synthetic_flag_fail_closed(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            fixture = build_repo(root)
            payload = json.loads(fixture["packet"].read_text(encoding="utf-8"))
            payload["gitIdentity"]["expectedGitSha"] = "f" * 40
            payload["gitIdentity"]["gitIdentityMatched"] = False
            payload["gitIdentity"]["exactSourceIdentitySatisfied"] = False
            write_json(fixture["packet"], payload)
            completed = self.invoke_verifier(root, fixture["packet"], fixture["heroRepo"])
            self.assertNotEqual(0, completed.returncode)
            self.assertIn("Git identity", completed.stderr + completed.stdout)

            payload["gitIdentity"]["expectedGitSha"] = fixture["sha"]
            payload["gitIdentity"]["gitIdentityMatched"] = True
            payload["gitIdentity"]["exactSourceIdentitySatisfied"] = True
            payload["gitIdentity"]["syntheticPullRequestMerge"] = True
            payload["gitIdentity"]["status"] = "SYNTHETIC_PR_MERGE_REF"
            write_json(fixture["packet"], payload)
            completed = self.invoke_verifier(root, fixture["packet"], fixture["heroRepo"])
            self.assertNotEqual(0, completed.returncode)
            self.assertIn("not exact/non-synthetic", completed.stderr + completed.stdout)

    def test_operator_chain_digest_and_hero_source_mismatch_fail_closed(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            fixture = build_repo(root)
            payload = json.loads(fixture["packet"].read_text(encoding="utf-8"))
            payload["operatorChain"]["sha256"] = "0" * 64
            write_json(fixture["packet"], payload)
            completed = self.invoke_verifier(root, fixture["packet"], fixture["heroRepo"])
            self.assertNotEqual(0, completed.returncode)
            self.assertIn("operator-chain SHA-256 mismatch", completed.stderr + completed.stdout)

        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            fixture = build_repo(root)
            completed = self.invoke_verifier(
                root,
                fixture["packet"],
                "Assets/Afareet/ArtSource/Vehicles/HeroCar/DifferentHero.fbx",
            )
            self.assertNotEqual(0, completed.returncode)
            self.assertIn("Hero source mismatch", completed.stderr + completed.stdout)

    def test_not_ready_or_self_promoting_packet_is_rejected_before_staging(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            fixture = build_repo(root)
            payload = json.loads(fixture["packet"].read_text(encoding="utf-8"))
            payload["state"] = "BLOCKED_GIT_IDENTITY"
            payload["releaseHandoffEligible"] = False
            write_json(fixture["packet"], payload)
            completed = self.invoke_verifier(root, fixture["packet"], fixture["heroRepo"])
            self.assertNotEqual(0, completed.returncode)
            self.assertIn("READY_FOR_LICENSED_OPERATOR_HANDOFF", completed.stderr + completed.stdout)

        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            fixture = build_repo(root)
            payload = json.loads(fixture["packet"].read_text(encoding="utf-8"))
            payload["verified"] = True
            write_json(fixture["packet"], payload)
            completed = self.invoke_verifier(root, fixture["packet"], fixture["heroRepo"])
            self.assertNotEqual(0, completed.returncode)
            self.assertIn("handoff.verified must remain JSON false", completed.stderr + completed.stdout)

    def test_authoritative_runner_verifies_packet_before_low_level_staging(self):
        text = RUNNER_SCRIPT.read_text(encoding="utf-8")
        verify_reference = text.index("verify_p1_licensed_handoff_packet_windows.ps1")
        verify_call = text.index("& $verifyScript")
        verify_ok = text.index("AFAREET_P1_LICENSED_STAGING_PACKET_VERIFY_OK")
        stage_reference = text.index("stage_production_candidate_windows.ps1")
        stage_call = text.index("& $stageScript")
        self.assertLess(verify_reference, stage_reference)
        self.assertLess(verify_call, verify_ok)
        self.assertLess(verify_ok, stage_call)
        self.assertNotIn("Unity.exe", text)
        self.assertNotIn("Start-Process", text)
        self.assertIn("publicationPerformed -ne $false", text)
        self.assertIn("verified -ne $false", text)


if __name__ == "__main__":
    unittest.main()
