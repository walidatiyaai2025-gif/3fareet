import hashlib
import importlib.util
import json
import subprocess
import tempfile
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]


def _load_module():
    path = TOOLS_DIR / "verify_p1_production_art.py"
    spec = importlib.util.spec_from_file_location("verify_p1_production_art", path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


GATE = _load_module()


def _sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _git(repo: Path, *args: str) -> str:
    completed = subprocess.run(
        ["git", "-C", str(repo), *args],
        check=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )
    return completed.stdout.strip()


def _hashed_record(repo: Path, path: Path) -> dict[str, str]:
    return {"path": path.relative_to(repo).as_posix(), "sha256": _sha256(path)}


class ProductionArtGateTests(unittest.TestCase):
    def _fixture(self, root: Path):
        repo = root / "repo"
        repo.mkdir()
        evidence_dir = root / "evidence"
        evidence_dir.mkdir()
        apk_sha = "b" * 64

        spec_payload = json.loads((TOOLS_DIR / "p1_production_art_spec.json").read_text(encoding="utf-8"))
        tasks = spec_payload["requiredTasks"]
        uart004_policy = spec_payload["taskArtifactPolicies"]["UART-004"]
        assets = {}

        for index, task_id in enumerate(tasks):
            shot = evidence_dir / f"{task_id.lower()}.png"
            shot.write_bytes(f"png-evidence-{task_id}".encode("utf-8"))

            if task_id == "UART-003":
                source = repo / "unity_game/Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx"
                runtime = repo / "unity_game/Assets/UART-003/runtime_0.prefab"
                source.parent.mkdir(parents=True, exist_ok=True)
                runtime.parent.mkdir(parents=True, exist_ok=True)
                source.write_bytes(b"authored-model-UART-003")
                runtime.write_text("prefab-UART-003", encoding="utf-8")
                source_files = [_hashed_record(repo, source)]
                runtime_assets = [_hashed_record(repo, runtime)]
            elif task_id == "UART-004":
                source_files = []
                runtime_assets = []
                for source_relative in uart004_policy["exactAuthored3DSourcePaths"]:
                    source = repo / source_relative
                    source.parent.mkdir(parents=True, exist_ok=True)
                    source.write_bytes(f"authored-rival-{source.name}".encode("utf-8"))
                    source_files.append(_hashed_record(repo, source))
                for runtime_relative in uart004_policy["requiredRuntimeAssetPaths"]:
                    runtime = repo / runtime_relative
                    runtime.parent.mkdir(parents=True, exist_ok=True)
                    runtime.write_text(f"prefab-{runtime.name}", encoding="utf-8")
                    runtime_assets.append(_hashed_record(repo, runtime))
            else:
                source = repo / f"Assets/{task_id}/source/model_{index}.fbx"
                runtime = repo / f"unity_game/Assets/{task_id}/runtime_{index}.prefab"
                source.parent.mkdir(parents=True, exist_ok=True)
                runtime.parent.mkdir(parents=True, exist_ok=True)
                source.write_bytes(f"authored-model-{task_id}".encode("utf-8"))
                runtime.write_text(f"prefab-{task_id}", encoding="utf-8")
                source_files = [_hashed_record(repo, source)]
                runtime_assets = [_hashed_record(repo, runtime)]

            assets[task_id] = {
                "reviewState": "ACCEPTED",
                "quality": "production",
                "authored3D": True,
                "runtimeActive": True,
                "proceduralFallbackActive": False,
                "ownerAccepted": True,
                "sourceFiles": source_files,
                "runtimeAssets": runtime_assets,
                "evidence": [{"kind": "screenshot", "path": shot.name, "sha256": _sha256(shot)}],
            }

        _git(repo, "init")
        _git(repo, "config", "user.name", "AFAREET Test")
        _git(repo, "config", "user.email", "afareet-test@example.invalid")
        _git(repo, "add", "Assets", "unity_game")
        _git(repo, "commit", "-m", "production art fixture")
        git_sha = _git(repo, "rev-parse", "HEAD").lower()

        manifest = {
            "schemaVersion": 2,
            "visualGate": "UPER-009",
            "verified": False,
            "ownerAccepted": True,
            "candidate": {"gitSha": git_sha, "apkSha256": apk_sha},
            "fallbackState": {
                "heroProcedural": False,
                "rivalsProcedural": False,
                "trackProcedural": False,
                "cairoWorldProcedural": False,
                "landmarksProcedural": False,
            },
            "assets": assets,
        }
        manifest_path = evidence_dir / "p1-production-art.json"
        manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
        return repo, manifest_path, manifest, git_sha, apk_sha

    def _verify(self, repo, manifest_path, git_sha, apk_sha):
        return GATE.verify_art_manifest(
            manifest_path=manifest_path,
            repo_root=repo,
            spec_path=TOOLS_DIR / "p1_production_art_spec.json",
            expected_git_sha=git_sha,
            expected_apk_sha=apk_sha,
        )

    def test_complete_production_art_manifest_passes_but_never_verified(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, _manifest, git_sha, apk_sha = self._fixture(Path(directory))
            result = self._verify(repo, path, git_sha, apk_sha)
            self.assertEqual("PRODUCTION_ART_GATE_PASSED", result["verdict"])
            self.assertFalse(result["verified"])
            self.assertEqual(2, result["schemaVersion"])
            self.assertEqual(6, len(result["acceptedTasks"]))
            self.assertEqual(6, result["evidenceCount"])
            self.assertEqual(22, result["artifactFingerprintCount"])
            self.assertTrue(result["artifactFingerprintsVerified"])
            self.assertTrue(result["gitCandidateHeadVerified"])
            self.assertEqual(16, result["gitTrackedArtifactCount"])
            self.assertTrue(result["gitTrackedArtifactsVerified"])
            self.assertTrue(result["taskArtifactPolicyVerified"])
            self.assertFalse(result["proceduralFallbackAccepted"])

    def test_blockout_quality_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            manifest["assets"]["UART-005"]["quality"] = "blockout"
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, "UART-005 is not production quality"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_active_procedural_world_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            manifest["fallbackState"]["cairoWorldProcedural"] = True
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, "cairoWorldProcedural is active"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_task_level_fallback_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            manifest["assets"]["UART-003"]["proceduralFallbackActive"] = True
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, "UART-003 is still using procedural/blockout fallback"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_candidate_fingerprint_mismatch_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, _manifest, _git_sha, apk_sha = self._fixture(Path(directory))
            with self.assertRaisesRegex(GATE.ProductionArtGateError, "Git SHA does not match"):
                self._verify(repo, path, "c" * 40, apk_sha)

    def test_missing_runtime_asset_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            runtime_rel = manifest["assets"]["UART-006"]["runtimeAssets"][0]["path"]
            (repo / runtime_rel).unlink()
            with self.assertRaisesRegex(GATE.ProductionArtGateError, r"runtimeAssets\[0\] file is missing"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_manifest_cannot_self_assert_verified(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            manifest["verified"] = True
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, "must never self-assert VERIFIED"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_legacy_unhashed_schema_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            manifest["schemaVersion"] = 1
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, "schemaVersion must be 2"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_source_sha_mismatch_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            source_rel = manifest["assets"]["UART-003"]["sourceFiles"][0]["path"]
            (repo / source_rel).write_bytes(b"tampered-after-review")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, r"UART-003 sourceFiles\[0\] SHA-256 mismatch"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_runtime_sha_mismatch_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            runtime_rel = manifest["assets"]["UART-004"]["runtimeAssets"][0]["path"]
            (repo / runtime_rel).write_text("tampered-prefab", encoding="utf-8")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, r"UART-004 runtimeAssets\[0\] SHA-256 mismatch"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_evidence_sha_mismatch_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            shot_rel = manifest["assets"]["UART-005"]["evidence"][0]["path"]
            (path.parent / shot_rel).write_bytes(b"replacement-screenshot")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, r"UART-005 evidence\[0\] SHA-256 mismatch"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_missing_or_invalid_artifact_sha_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            del manifest["assets"]["UART-006"]["sourceFiles"][0]["sha256"]
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, r"UART-006 sourceFiles\[0\] sha256 must be 64 hex"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_generated_preview_source_path_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            repo, path, manifest, git_sha, apk_sha = self._fixture(root)
            generated = repo / "Assets/UART-003/Generated/model.fbx"
            generated.parent.mkdir(parents=True, exist_ok=True)
            generated.write_bytes(b"generated-preview")
            manifest["assets"]["UART-003"]["sourceFiles"] = [{
                "path": generated.relative_to(repo).as_posix(),
                "sha256": _sha256(generated),
            }]
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, "forbidden generated/preview/blockout source segment: generated"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_uart003_rival_source_is_rejected_by_task_policy(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            manifest["assets"]["UART-003"]["sourceFiles"] = [
                dict(manifest["assets"]["UART-004"]["sourceFiles"][0])
            ]
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(
                GATE.ProductionArtGateError,
                "UART-003 authored 3D source uses forbidden role segment: rivals",
            ):
                self._verify(repo, path, git_sha, apk_sha)

    def test_uart003_nonvehicle_source_is_rejected_by_task_policy(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            manifest["assets"]["UART-003"]["sourceFiles"] = [
                dict(manifest["assets"]["UART-005"]["sourceFiles"][0])
            ]
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(
                GATE.ProductionArtGateError,
                "UART-003 authored 3D source is outside required role segment: vehicles",
            ):
                self._verify(repo, path, git_sha, apk_sha)

    def test_uart004_exact_source_set_is_required(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            manifest["assets"]["UART-004"]["sourceFiles"] = manifest["assets"]["UART-004"]["sourceFiles"][:-1]
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(
                GATE.ProductionArtGateError,
                "UART-004 authored 3D source set must exactly match production policy",
            ):
                self._verify(repo, path, git_sha, apk_sha)

    def test_uart004_required_runtime_prefabs_are_required(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            manifest["assets"]["UART-004"]["runtimeAssets"] = manifest["assets"]["UART-004"]["runtimeAssets"][:-1]
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(
                GATE.ProductionArtGateError,
                "UART-004 required production runtime asset is missing from evidence",
            ):
                self._verify(repo, path, git_sha, apk_sha)

    def test_visual_evidence_cannot_be_reused_across_tasks(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            manifest["assets"]["UART-004"]["evidence"] = [dict(manifest["assets"]["UART-003"]["evidence"][0])]
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, "visual evidence file is reused"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_repository_head_must_match_manifest_candidate(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            marker = repo / "unrelated.txt"
            marker.write_text("new commit", encoding="utf-8")
            _git(repo, "add", "unrelated.txt")
            _git(repo, "commit", "-m", "advance head")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, "repository HEAD does not match"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_repo_artifact_must_be_tracked_by_candidate(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            untracked = repo / "unity_game/Assets/UART-004/untracked.prefab"
            untracked.parent.mkdir(parents=True, exist_ok=True)
            untracked.write_bytes(b"untracked-runtime")
            manifest["assets"]["UART-004"]["runtimeAssets"].append({
                "path": untracked.relative_to(repo).as_posix(),
                "sha256": _sha256(untracked),
            })
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, "is not tracked by candidate Git commit"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_working_tree_artifact_cannot_differ_from_candidate_blob_even_with_updated_manifest_hash(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, git_sha, apk_sha = self._fixture(Path(directory))
            source_item = manifest["assets"]["UART-003"]["sourceFiles"][0]
            source = repo / source_item["path"]
            source.write_bytes(b"different-working-tree-source")
            source_item["sha256"] = _sha256(source)
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, "working-tree bytes do not match candidate Git blob"):
                self._verify(repo, path, git_sha, apk_sha)

    def test_repo_root_must_be_exact_git_worktree_root(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, _manifest, git_sha, apk_sha = self._fixture(Path(directory))
            nested = repo / "Assets"
            with self.assertRaisesRegex(GATE.ProductionArtGateError, "exact Git worktree root"):
                GATE._verify_git_candidate_binding(nested, git_sha, [])


if __name__ == "__main__":
    unittest.main()
