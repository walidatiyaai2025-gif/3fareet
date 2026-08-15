import importlib.util
import json
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


class ProductionArtGateTests(unittest.TestCase):
    def _fixture(self, root: Path):
        repo = root / "repo"
        repo.mkdir()
        evidence_dir = root / "evidence"
        evidence_dir.mkdir()
        git_sha = "a" * 40
        apk_sha = "b" * 64

        tasks = json.loads((TOOLS_DIR / "p1_production_art_spec.json").read_text(encoding="utf-8"))["requiredTasks"]
        assets = {}
        for index, task_id in enumerate(tasks):
            source = repo / f"Assets/{task_id}/source/model_{index}.fbx"
            runtime = repo / f"unity_game/Assets/{task_id}/runtime_{index}.prefab"
            shot = evidence_dir / f"{task_id.lower()}.png"
            source.parent.mkdir(parents=True, exist_ok=True)
            runtime.parent.mkdir(parents=True, exist_ok=True)
            source.write_bytes(b"authored-model")
            runtime.write_text("prefab", encoding="utf-8")
            shot.write_bytes(b"png-evidence")
            assets[task_id] = {
                "reviewState": "ACCEPTED",
                "quality": "production",
                "authored3D": True,
                "runtimeActive": True,
                "proceduralFallbackActive": False,
                "ownerAccepted": True,
                "sourceFiles": [source.relative_to(repo).as_posix()],
                "runtimeAssets": [runtime.relative_to(repo).as_posix()],
                "evidence": [{"kind": "screenshot", "path": shot.name}],
            }

        manifest = {
            "schemaVersion": 1,
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
            self.assertEqual(6, len(result["acceptedTasks"]))
            self.assertEqual(6, result["evidenceCount"])
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
            runtime_rel = manifest["assets"]["UART-006"]["runtimeAssets"][0]
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


if __name__ == "__main__":
    unittest.main()
