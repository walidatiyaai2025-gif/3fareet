import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "verify_unity_package_lock.py"
SPEC = importlib.util.spec_from_file_location("verify_unity_package_lock", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


def write_json(path: Path, payload: dict) -> None:
    path.write_text(json.dumps(payload), encoding="utf-8")


class VerifyUnityPackageLockTests(unittest.TestCase):
    def setUp(self) -> None:
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.manifest = self.root / "manifest.json"
        self.lock = self.root / "packages-lock.json"

        self.manifest_payload = {
            "dependencies": {
                "com.unity.inputsystem": "1.19.0",
                "com.unity.ugui": "2.5.0",
                "com.unity.modules.vehicles": "1.0.0",
            }
        }
        self.lock_payload = {
            "dependencies": {
                "com.unity.inputsystem": {
                    "version": "1.19.0",
                    "depth": 0,
                    "source": "registry",
                    "dependencies": {"com.unity.modules.uielements": "1.0.0"},
                },
                "com.unity.ugui": {
                    "version": "2.5.0",
                    "depth": 0,
                    "source": "builtin",
                    "dependencies": {
                        "com.unity.modules.ui": "1.0.0",
                        "com.unity.modules.imgui": "1.0.0",
                        "com.unity.modules.audio": "1.0.0",
                        "com.unity.modules.physics2d": "1.0.0",
                        "com.unity.modules.physics": "1.0.0",
                    },
                },
                "com.unity.modules.vehicles": {
                    "version": "1.0.0",
                    "depth": 0,
                    "source": "builtin",
                    "dependencies": {"com.unity.modules.physics": "1.0.0"},
                },
                "com.unity.modules.uielements": {
                    "version": "1.0.0",
                    "depth": 1,
                    "source": "builtin",
                    "dependencies": {},
                },
                "com.unity.modules.ui": {
                    "version": "1.0.0",
                    "depth": 1,
                    "source": "builtin",
                    "dependencies": {},
                },
                "com.unity.modules.imgui": {
                    "version": "1.0.0",
                    "depth": 1,
                    "source": "builtin",
                    "dependencies": {},
                },
                "com.unity.modules.audio": {
                    "version": "1.0.0",
                    "depth": 1,
                    "source": "builtin",
                    "dependencies": {},
                },
                "com.unity.modules.physics2d": {
                    "version": "1.0.0",
                    "depth": 1,
                    "source": "builtin",
                    "dependencies": {"com.unity.modules.physicscore2d": "1.0.0"},
                },
                "com.unity.modules.physics": {
                    "version": "1.0.0",
                    "depth": 1,
                    "source": "builtin",
                    "dependencies": {},
                },
            }
        }

    def tearDown(self) -> None:
        self.tmp.cleanup()

    def verify(self):
        write_json(self.manifest, self.manifest_payload)
        write_json(self.lock, self.lock_payload)
        return MODULE.verify(self.manifest, self.lock)

    def test_valid_direct_dependencies_pass(self):
        checked = self.verify()
        self.assertEqual(
            checked,
            ["com.unity.inputsystem", "com.unity.ugui", "com.unity.modules.vehicles"],
        )

    def test_missing_direct_dependency_is_rejected(self):
        del self.lock_payload["dependencies"]["com.unity.ugui"]
        with self.assertRaisesRegex(MODULE.PackageLockError, "missing from lock"):
            self.verify()

    def test_direct_version_mismatch_is_rejected(self):
        self.lock_payload["dependencies"]["com.unity.inputsystem"]["version"] = "1.18.0"
        with self.assertRaisesRegex(MODULE.PackageLockError, "version mismatch"):
            self.verify()

    def test_nonzero_direct_depth_is_rejected(self):
        self.lock_payload["dependencies"]["com.unity.modules.vehicles"]["depth"] = 1
        with self.assertRaisesRegex(MODULE.PackageLockError, "depth 0"):
            self.verify()

    def test_known_child_dependency_contract_is_rejected(self):
        self.lock_payload["dependencies"]["com.unity.ugui"]["dependencies"] = {
            "com.unity.modules.ui": "1.0.0"
        }
        with self.assertRaisesRegex(MODULE.PackageLockError, "known dependency contract mismatch"):
            self.verify()

    def test_missing_resolved_child_dependency_is_rejected(self):
        del self.lock_payload["dependencies"]["com.unity.modules.physics2d"]
        with self.assertRaisesRegex(MODULE.PackageLockError, "resolved child dependency missing"):
            self.verify()

    def test_resolved_child_version_mismatch_is_rejected(self):
        self.lock_payload["dependencies"]["com.unity.modules.audio"]["version"] = "9.9.9"
        with self.assertRaisesRegex(MODULE.PackageLockError, "resolved child dependency version mismatch"):
            self.verify()


if __name__ == "__main__":
    unittest.main()
