import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]


def _load(name: str, filename: str):
    path = TOOLS_DIR / filename
    spec = importlib.util.spec_from_file_location(name, path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


FINGERPRINT = _load("fingerprint_p1_production_art_manifest", "fingerprint_p1_production_art_manifest.py")
GATE = FINGERPRINT.gate


class P1ProductionArtFingerprintTests(unittest.TestCase):
    def _fixture(self, root: Path):
        repo = root / "repo"
        repo.mkdir()
        evidence = root / "evidence"
        evidence.mkdir()
        git_sha = "a" * 40
        apk_sha = "b" * 64

        required = json.loads((TOOLS_DIR / "p1_production_art_spec.json").read_text(encoding="utf-8"))["requiredTasks"]
        assets = {}
        for index, task_id in enumerate(required):
            source = repo / f"Assets/{task_id}/source/model_{index}.obj"
            runtime = repo / f"unity_game/Assets/{task_id}/runtime_{index}.prefab"
            shot = evidence / f"{task_id.lower()}.png"
            source.parent.mkdir(parents=True, exist_ok=True)
            runtime.parent.mkdir(parents=True, exist_ok=True)
            source.write_bytes(f"source-{task_id}".encode())
            runtime.write_bytes(f"runtime-{task_id}".encode())
            shot.write_bytes(f"evidence-{task_id}".encode())
            assets[task_id] = {
                "reviewState": "ACCEPTED",
                "quality": "production",
                "authored3D": True,
                "runtimeActive": True,
                "proceduralFallbackActive": False,
                "ownerAccepted": True,
                "sourceFiles": [{"path": source.relative_to(repo).as_posix()}],
                "runtimeAssets": [{"path": runtime.relative_to(repo).as_posix()}],
                "evidence": [{"kind": "screenshot", "path": shot.name}],
            }

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
        path = evidence / "p1-production-art-template.json"
        path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
        return repo, path, manifest, git_sha, apk_sha

    def test_fingerprinted_manifest_is_deterministic_and_passes_gate(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, original, git_sha, apk_sha = self._fixture(Path(directory))
            first, first_count = FINGERPRINT.fingerprint_manifest(manifest_path=path, repo_root=repo)
            second, second_count = FINGERPRINT.fingerprint_manifest(manifest_path=path, repo_root=repo)

            self.assertEqual(first, second)
            self.assertEqual(18, first_count)
            self.assertEqual(first_count, second_count)
            self.assertNotIn("sha256", original["assets"]["UART-003"]["sourceFiles"][0])

            output = path.parent / "p1-production-art.json"
            FINGERPRINT.write_fingerprinted_manifest(input_path=path, output_path=output, payload=first)
            result = GATE.verify_art_manifest(
                manifest_path=output,
                repo_root=repo,
                expected_git_sha=git_sha,
                expected_apk_sha=apk_sha,
            )
            self.assertTrue(result["artifactFingerprintsVerified"])
            self.assertEqual(18, result["artifactFingerprintCount"])

    def test_tamper_after_fingerprinting_is_rejected_by_gate(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, _original, git_sha, apk_sha = self._fixture(Path(directory))
            payload, _count = FINGERPRINT.fingerprint_manifest(manifest_path=path, repo_root=repo)
            output = path.parent / "p1-production-art.json"
            FINGERPRINT.write_fingerprinted_manifest(input_path=path, output_path=output, payload=payload)

            source_rel = payload["assets"]["UART-003"]["sourceFiles"][0]["path"]
            (repo / source_rel).write_bytes(b"changed-after-fingerprint")
            with self.assertRaisesRegex(GATE.ProductionArtGateError, "SHA-256 mismatch"):
                GATE.verify_art_manifest(
                    manifest_path=output,
                    repo_root=repo,
                    expected_git_sha=git_sha,
                    expected_apk_sha=apk_sha,
                )

    def test_missing_declared_artifact_fails_closed(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, _git_sha, _apk_sha = self._fixture(Path(directory))
            runtime_rel = manifest["assets"]["UART-006"]["runtimeAssets"][0]["path"]
            (repo / runtime_rel).unlink()
            with self.assertRaisesRegex(GATE.ProductionArtGateError, "file is missing"):
                FINGERPRINT.fingerprint_manifest(manifest_path=path, repo_root=repo)

    def test_fingerprinter_refuses_verified_input(self):
        with tempfile.TemporaryDirectory() as directory:
            repo, path, manifest, _git_sha, _apk_sha = self._fixture(Path(directory))
            manifest["verified"] = True
            path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaisesRegex(FINGERPRINT.FingerprintManifestError, "self-assert VERIFIED"):
                FINGERPRINT.fingerprint_manifest(manifest_path=path, repo_root=repo)

    def test_output_must_stay_beside_input_and_never_overwrite(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            _repo, path, manifest, _git_sha, _apk_sha = self._fixture(root)
            with self.assertRaisesRegex(FINGERPRINT.FingerprintManifestError, "never overwrites the input"):
                FINGERPRINT.write_fingerprinted_manifest(input_path=path, output_path=path, payload=manifest)

            elsewhere = root / "other"
            elsewhere.mkdir()
            with self.assertRaisesRegex(FINGERPRINT.FingerprintManifestError, "must stay beside the input"):
                FINGERPRINT.write_fingerprinted_manifest(
                    input_path=path,
                    output_path=elsewhere / "p1-production-art.json",
                    payload=manifest,
                )

            existing = path.parent / "existing.json"
            existing.write_text("{}", encoding="utf-8")
            with self.assertRaisesRegex(FINGERPRINT.FingerprintManifestError, "refusing to overwrite"):
                FINGERPRINT.write_fingerprinted_manifest(input_path=path, output_path=existing, payload=manifest)


if __name__ == "__main__":
    unittest.main()
